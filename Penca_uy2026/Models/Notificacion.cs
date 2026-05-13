using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Notificaciones generadas por el sistema para los usuarios de un sitio.
    /// </summary>
    public class Notificacion : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el usuario ya ha visualizado la notificación.
        /// </summary>
        public bool FueLeida { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // --- RELACIONES ---

        [Required]
        public int UsuarioSitioId { get; set; }

        public int SitioId { get; set; }

        [ForeignKey("UsuarioSitioId")]
        public UsuarioSitio UsuarioSitio { get; set; } = null!;
    }
}
