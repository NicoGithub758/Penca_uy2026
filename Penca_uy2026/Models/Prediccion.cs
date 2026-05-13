using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Representa el pronóstico de un usuario para un partido específico.
    /// </summary>
    public class Prediccion : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Puntos obtenidos por esta predicción específica una vez finalizado el partido.
        /// </summary>
        public int PuntosObtenidos { get; set; } = 0;

        [Required]
        public int GolesEquipoLocal { get; set; }

        [Required]
        public int GolesEquipoVisitante { get; set; }

        // --- RELACIONES ---

        [Required]
        public int ParticipacionId { get; set; }

        public int SitioId { get; set; }

        [ForeignKey("ParticipacionId")]
        public Participacion Participacion { get; set; } = null!;

        /// <summary>
        /// Referencia al partido (modelo de los compañeros) sobre el cual se hace la apuesta.
        /// </summary>
        [Required]
        public int PartidoId { get; set; }
        
        [ForeignKey("PartidoId")]
        public Partido Partido { get; set; } = null!;
    }
}
