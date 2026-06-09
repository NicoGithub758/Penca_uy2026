using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Penca_uy2026.Interfaces;

namespace Penca_uy2026.Models
{
    public class Partido
    {
        public int Id { get; set; }
        public int PencaId { get; set; }
        public int LocalEquipoId { get; set; }
        public int VisitanteEquipoId { get; set; }
        public int? ApiFootballFixtureId { get; set; }
        public int? GolesLocal { get; set; }
        public int? GolesVisitante { get; set; }
        public DateTime Fecha { get; set; }
        public string Fase { get; set; } = "Regular"; // Ej: "Octavos", "Cuartos", "Final"
        public bool Jugado { get; set; } = false;
        public string? ApiFootballStatus { get; set; }
        public DateTime? UltimaConsultaApi { get; set; }
        public int IntentosConsultaApi { get; set; }

        
        // ----------------- RELACIONES -------------------
        [ForeignKey("LocalEquipoId")]
        public Equipo Local { get; set; } = null!;

        [ForeignKey("VisitanteEquipoId")]
        public Equipo Visitante { get; set; } = null!;

        [ForeignKey("PencaId")]
        public Penca Penca { get; set; } = null!;

    }
}
