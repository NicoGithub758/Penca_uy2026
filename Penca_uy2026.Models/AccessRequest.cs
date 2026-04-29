using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    // RequestStatus: Enum para manejar si está pendiente, aprobada o rechazada
    public enum RequestStatus { Pending, Approved, Rejected }

    public class AccessRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        // --- Relaciones ---
        [Required]
        public int SiteId { get; set; }
        [ForeignKey("SiteId")]
        public Site Site { get; set; } = null!;
    }
}