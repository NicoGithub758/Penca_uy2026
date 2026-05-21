using System.Text.Json.Serialization;

namespace Penca_uy2026.DTOs
{
    public class ApiFootballTeamsResponse
    {
        [JsonPropertyName("results")]
        public int Results { get; set; }

        [JsonPropertyName("response")]
        public List<ApiFootballTeamResponseItem> Response { get; set; } = new();
    }

    public class ApiFootballTeamResponseItem
    {
        [JsonPropertyName("team")]
        public ApiFootballTeamDto Team { get; set; } = new();
    }

    public class ApiFootballTeamDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("logo")]
        public string? Logo { get; set; }
    }
}