using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; } = string.Empty; // Content: El texto del mensaje

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // --- Relaciones ---

        [Required]
        public int ParticipationId { get; set; }
        [ForeignKey("ParticipationId")]
        public Participation Participation { get; set; } = null!;
    }
}