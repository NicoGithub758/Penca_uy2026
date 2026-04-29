using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false; // IsRead: Para saber si el usuario ya vio la notificación.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Relaciones ---
        [Required]
        public int SiteUserId { get; set; }
        [ForeignKey("SiteUserId")]
        public SiteUser SiteUser { get; set; } = null!;
    }
}