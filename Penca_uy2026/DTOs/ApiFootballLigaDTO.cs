namespace Penca_uy2026.DTOs
{
    public class ApiFootballLigaDto
    {
        public int LeagueId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }

        public string Pais { get; set; } = string.Empty;
        public string? BanderaUrl { get; set; }

        public int Temporada { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }
}