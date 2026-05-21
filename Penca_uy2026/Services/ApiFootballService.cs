using System.Net.Http.Json;
using Penca_uy2026.DTOs;

namespace Penca_uy2026.Services
{
    public class ApiFootballService
    {
        private readonly HttpClient _httpClient;

        public ApiFootballService(IConfiguration config)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://v3.football.api-sports.io/")
            };

            var apiKey = config["ApiFootball:ApiKey"]
                ?? throw new ArgumentNullException("ApiFootball:ApiKey");

            _httpClient.DefaultRequestHeaders.Add("x-apisports-key", apiKey);
        }

        public async Task<List<ApiFootballTeamDto>> GetTeamsAsync(int leagueId, int season)
        {
            var result = await _httpClient.GetFromJsonAsync<ApiFootballTeamsResponse>(
                $"teams?league={leagueId}&season={season}");

            return result?.Response
                .Select(x => x.Team)
                .ToList() ?? new List<ApiFootballTeamDto>();
        }
    }
}