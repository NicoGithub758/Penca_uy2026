using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class Prediction
    {
        [Key]
        public int Id { get; set; }

        // Puntos obtenidos por esta predicción luego de finalizado el evento
        public int Points { get; set; } = 0;

        // Cantidad de goles predichos para el equipo local
        [Required]
        public int HomeTeamScore { get; set; }

        // Cantidad de goles predichos para el equipo visitante
        [Required]
        public int AwayTeamScore { get; set; }

        // --- Relaciones ---

        [Required]
        public int ParticipationId { get; set; }
        [ForeignKey("ParticipationId")]
        public Participation Participation { get; set; } = null!;

        [Required]
        public int EventId { get; set; }
        [ForeignKey("EventId")]
        public Event Event { get; set; } = null!;
    }
}