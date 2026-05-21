namespace Penca_uy2026.Models
{
    public class Equipo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int? ApiFootballTeamId { get; set; }
        public string? LogoUrl { get; set; }
        public int PencaId { get; set; }
    }
}
