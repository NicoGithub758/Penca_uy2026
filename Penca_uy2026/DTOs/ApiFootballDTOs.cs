using System.Text.Json.Serialization;

namespace Penca_uy2026.DTOs
{
    public class ApiFootballResponse<T>
    {
        [JsonPropertyName("response")]
        public List<T> Response { get; set; } = new();

        [JsonPropertyName("errors")]
        public object? Errors { get; set; }
    }

    public class ApiFootballFixtureItem
    {
        [JsonPropertyName("fixture")]
        public ApiFootballFixture Fixture { get; set; } = new();

        [JsonPropertyName("teams")]
        public ApiFootballTeams Teams { get; set; } = new();

        [JsonPropertyName("goals")]
        public ApiFootballGoals Goals { get; set; } = new();
    }

    public class ApiFootballFixture
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("status")]
        public ApiFootballStatus Status { get; set; } = new();
    }

    public class ApiFootballStatus
    {
        [JsonPropertyName("short")]
        public string? Short { get; set; }

        [JsonPropertyName("elapsed")]
        public int? Elapsed { get; set; }
    }

    public class ApiFootballTeams
    {
        [JsonPropertyName("home")]
        public ApiFootballTeam Home { get; set; } = new();

        [JsonPropertyName("away")]
        public ApiFootballTeam Away { get; set; } = new();
    }

    public class ApiFootballTeam
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class ApiFootballGoals
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }

        [JsonPropertyName("away")]
        public int? Away { get; set; }
    }
}
