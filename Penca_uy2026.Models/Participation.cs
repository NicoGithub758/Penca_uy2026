using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class Participation
    {
        [Key]
        public int Id { get; set; }

        // Indica si el usuario ya abonó el costo de la entrada a la penca (Requerimiento CU05)
        public bool IsPaid { get; set; } = false;
        
        [Required]
        public int TotalPoints { get; set; } = 0;

        // --- Relaciones ---

        [Required]
        public int SiteUserId { get; set; }
        [ForeignKey("SiteUserId")]
        public SiteUser SiteUser { get; set; } = null!;

        [Required]
        public int PencaInstanceId { get; set; }
        [ForeignKey("PencaInstanceId")]
        public PencaInstance PencaInstance { get; set; } = null!;

        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}