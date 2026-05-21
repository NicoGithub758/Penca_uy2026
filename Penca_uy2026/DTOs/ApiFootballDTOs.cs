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

    public class ApiFootballFixturesResponse
    {
        [JsonPropertyName("response")]
        public List<ApiFootballFixtureItem> Response { get; set; } = new();
    }

    public class ApiFootballFixtureItem
    {
        [JsonPropertyName("fixture")]
        public ApiFootballFixture Fixture { get; set; } = new();

        [JsonPropertyName("teams")]
        public ApiFootballFixtureTeams Teams { get; set; } = new();

        [JsonPropertyName("goals")]
        public ApiFootballGoals Goals { get; set; } = new();
    }

    public class ApiFootballFixture
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("status")]
        public ApiFootballFixtureStatus Status { get; set; } = new();
    }

    public class ApiFootballFixtureStatus
    {
        [JsonPropertyName("short")]
        public string Short { get; set; } = string.Empty;
    }

    public class ApiFootballFixtureTeams
    {
        [JsonPropertyName("home")]
        public ApiFootballFixtureTeam Home { get; set; } = new();

        [JsonPropertyName("away")]
        public ApiFootballFixtureTeam Away { get; set; } = new();
    }

    public class ApiFootballFixtureTeam
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class ApiFootballGoals
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }

        [JsonPropertyName("away")]
        public int? Away { get; set; }
    }
}