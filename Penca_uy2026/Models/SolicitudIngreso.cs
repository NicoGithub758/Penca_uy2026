using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public enum EstadoSolicitud { Pendiente, Aprobada, Rechazada }

    /// <summary>
    /// Representa la solicitud de un usuario externo para unirse a un sitio con autorización.
    /// </summary>
    public class SolicitudIngreso
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

        // --- RELACIONES ---
        
        [Required]
        public int SitioId { get; set; }
        
        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;
    }
}
