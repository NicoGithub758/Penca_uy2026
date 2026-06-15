using System.Net.Http.Json;
using System.Text.Json;
using Penca_uy2026.DTOs;

namespace Penca_uy2026.Services
{
    public class ApiFootballService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiFootballService> _logger;
        private readonly ParametrosSistemaService _parametrosSistemaService;

        public ApiFootballService(
            IConfiguration config,
            ILogger<ApiFootballService> logger,
            ParametrosSistemaService parametrosSistemaService)
        {
            _logger = logger;
            _parametrosSistemaService = parametrosSistemaService;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://v3.football.api-sports.io/")
            };

            var apiKey = config["ApiFootball:ApiKey"]
                ?? throw new ArgumentNullException("ApiFootball:ApiKey");

            _httpClient.DefaultRequestHeaders.Add("x-apisports-key", apiKey);
        }

        public async Task<List<ApiFootballLigaDto>> ObtenerLigasActualesAsync()
        {
            var response = await _httpClient.GetAsync("/leagues?current=true");

            if (!response.IsSuccessStatusCode)
                return new List<ApiFootballLigaDto>();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetProperty("results").GetInt32() == 0)
                return new List<ApiFootballLigaDto>();

            var ligas = new List<ApiFootballLigaDto>();

            foreach (var item in root.GetProperty("response").EnumerateArray())
            {
                var league = item.GetProperty("league");
                var country = item.GetProperty("country");

                var temporadaActual = item.GetProperty("seasons")
                    .EnumerateArray()
                    .FirstOrDefault(s =>
                        s.TryGetProperty("current", out var current) &&
                        current.ValueKind == JsonValueKind.True);

                if (temporadaActual.ValueKind == JsonValueKind.Undefined)
                    continue;

                ligas.Add(new ApiFootballLigaDto
                {
                    LeagueId = league.GetProperty("id").GetInt32(),
                    Nombre = league.GetProperty("name").GetString() ?? "",
                    LogoUrl = league.GetProperty("logo").GetString(),
                    Tipo = league.GetProperty("type").GetString() ?? "",

                    Pais = country.GetProperty("name").GetString() ?? "",
                    BanderaUrl = country.GetProperty("flag").GetString(),

                    Temporada = temporadaActual.GetProperty("year").GetInt32()
                });
            }

            return ligas
                .OrderBy(l => l.Pais)
                .ThenBy(l => l.Nombre)
                .ToList();
        }

        public async Task<List<ApiFootballTeamDto>> GetTeamsAsync(int leagueId, int season)
        {
            var result = await _httpClient.GetFromJsonAsync<ApiFootballTeamsResponse>(
                $"teams?league={leagueId}&season={season}");

            return result?.Response
                .Select(x => x.Team)
                .ToList() ?? new List<ApiFootballTeamDto>();
        }

        public async Task<ApiFootballFixtureItem?> GetFixtureResultAsync(int leagueId, int season, int localTeamId,
                                                                            int visitanteTeamId, DateTime fecha)
        {
            var date = fecha.ToString("yyyy-MM-dd");
            var parametros = await _parametrosSistemaService.ObtenerAsync();
            var timezone = Uri.EscapeDataString(parametros.TimeZoneId);

            var result = await _httpClient.GetFromJsonAsync<ApiFootballFixturesResponse>(
                $"fixtures/headtohead?h2h={localTeamId}-{visitanteTeamId}&league={leagueId}&season={season}&date={date}&timezone={timezone}");

            return result?.Response.FirstOrDefault();
        }

        public async Task<List<ApiFootballFixtureItem>> GetFixturesByLeagueDateAsync(int leagueId, int season, DateTime fecha)
        {
            var date = fecha.ToString("yyyy-MM-dd");
            var parametros = await _parametrosSistemaService.ObtenerAsync();
            var timezone = Uri.EscapeDataString(parametros.TimeZoneId);

            var url = $"fixtures?league={leagueId}&season={season}&date={date}&timezone={timezone}";


            _logger.LogInformation("API-Football request: GET {Url}", url);

            var result = await _httpClient.GetFromJsonAsync<ApiFootballFixturesResponse>(url);

            var fixtures = result?.Response ?? new List<ApiFootballFixtureItem>();

            _logger.LogInformation(
                "API-Football response: GET {Url}. Fixtures={Count}",
                url,
                fixtures.Count);

            return fixtures;
        }
    }
}
