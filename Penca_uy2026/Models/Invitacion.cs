using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Representa un token de invitación enviado por correo para acceder a un sitio privado.
    /// </summary>
    public class Invitacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Código alfanumérico único para la URL de validación.
        /// </summary>
        [Required]
        public string Token { get; set; } = Guid.NewGuid().ToString();

        // --- RELACIONES ---
        
        [Required]
        public int SitioId { get; set; }
        
        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;
    }
}
