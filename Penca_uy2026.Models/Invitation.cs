using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class Invitation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        // Token: Una cadena alfanumérica única para la URL de invitación
        [Required]
        public string Token { get; set; } = Guid.NewGuid().ToString();

        // --- Relaciones ---
        [Required]
        public int SiteId { get; set; }
        [ForeignKey("SiteId")]
        public Site Site { get; set; } = null!;
    }
}