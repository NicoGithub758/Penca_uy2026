using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe indicar un nombre")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha y hora del evento es obligatoria")]
        public DateTime StartDate { get; set; }

        // --- Relaciones ---

        [Required]
        public int PencaId { get; set; }

        [ForeignKey("PencaId")]
        public Penca Penca { get; set; } = null!;

        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();

        // Una relación para el equpio local y otra para el visitante.

        [Required]
        public int HomeTeamId {  get; set; }

        [ForeignKey("HomeTeamId")]
        public Team HomeTeam { get; set; } = null!;

        [Required]
        public int AwayTeamId { get; set; }

        [ForeignKey("AwayTeamId")]
        public Team AwayTeam { get; set; } = null!;
    }
}
