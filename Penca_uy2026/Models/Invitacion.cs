using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Representa un token de invitación enviado por correo para acceder a un sitio privado.
    /// </summary>
    public class Invitacion : IMultiTenant
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

        /// <summary>
        /// Para la invitación, controlamos por usos disponibles, para cubrir la posibilidad de que el link pueda ser usado por más de una persona.
        /// Por el momento, siempre será 1, en un futuro se puede agregar que la funcionalidad de crear link invitación, le pida al usuario cupos para el link.
        /// </summary>
        public int UsosDisponibles { get; set; } = 1;

        // --- RELACIONES ---
        
        [Required]
        public int SitioId { get; set; }
        
        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;
    }
}
