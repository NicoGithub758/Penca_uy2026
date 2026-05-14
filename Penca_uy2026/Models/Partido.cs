namespace Penca_uy2026.Models
{
    public class Partido
    {
        public int Id { get; set; }
        public int PencaId { get; set; }
        public string Local { get; set; } = string.Empty;
        public string Visitante { get; set; } = string.Empty;
        public int? GolesLocal { get; set; }
        public int? GolesVisitante { get; set; }
        public DateTime Fecha { get; set; }
        public string Fase { get; set; } = "Regular"; // Ej: "Octavos", "Cuartos", "Final"
        public bool Jugado { get; set; } = false;
        public int? ApiFootballFixtureId { get; set; }
        public string? EstadoApi { get; set; }
        public int? Minuto { get; set; }
        public DateTime? UltimaSyncApi { get; set; }
    }
}
