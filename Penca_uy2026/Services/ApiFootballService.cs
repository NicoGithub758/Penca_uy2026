using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;

namespace Penca_uy2026.Services
{
    public class ApiFootballService
    {
        private static readonly string[] FinalStatuses = { "FT", "AET", "PEN" };

        private readonly HttpClient _httpClient;
        private readonly ApiFootballOptions _options;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiFootballService(HttpClient httpClient, IOptions<ApiFootballOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<ApiFootballFixtureItem?> BuscarPartidoFinalizadoAsync(Partido partido)
        {
            var fecha = partido.Fecha.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var fixtures = await GetAsync<ApiFootballFixtureItem>($"fixtures?date={fecha}");

            return fixtures
                .Where(f => FinalStatuses.Contains(f.Fixture.Status.Short))
                .FirstOrDefault(f => SonLosMismosEquipos(partido, f));
        }

        private async Task<List<T>> GetAsync<T>(string path)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Falta configurar ApiFootball:ApiKey.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("x-apisports-key", _options.ApiKey);

            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"API-Football respondió {(int)response.StatusCode}: {content}");
            }

            var apiResponse = JsonSerializer.Deserialize<ApiFootballResponse<T>>(content, _jsonOptions);
            if (apiResponse == null)
            {
                throw new InvalidOperationException("No se pudo interpretar la respuesta de API-Football.");
            }

            if (TieneErrores(apiResponse.Errors))
            {
                throw new InvalidOperationException($"API-Football devolvió errores: {apiResponse.Errors}");
            }

            return apiResponse.Response;
        }

        private static bool SonLosMismosEquipos(Partido partido, ApiFootballFixtureItem fixture)
        {
            var local = Normalizar(partido.Local);
            var visitante = Normalizar(partido.Visitante);
            var apiLocal = Normalizar(fixture.Teams.Home.Name);
            var apiVisitante = Normalizar(fixture.Teams.Away.Name);

            return local == apiLocal && visitante == apiVisitante
                || local == apiVisitante && visitante == apiLocal;
        }

        public static bool NombresEquivalentes(string? left, string? right)
        {
            return Normalizar(left) == Normalizar(right);
        }

        private static string Normalizar(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var chars = normalized
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray();

            return string.Join(" ", new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static bool TieneErrores(object? errors)
        {
            if (errors == null)
            {
                return false;
            }

            if (errors is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Array => element.GetArrayLength() > 0,
                    JsonValueKind.Object => element.EnumerateObject().Any(),
                    JsonValueKind.String => !string.IsNullOrWhiteSpace(element.GetString()),
                    _ => false
                };
            }

            return true;
        }
    }
}
