using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Representa un usuario final registrado en un sitio específico.
    /// Un mismo usuario físico podría estar registrado en múltiples sitios con diferentes perfiles.
    /// </summary>
    public class UsuarioSitio
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debes ingresar un nombre")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes ingresar un correo electrónico")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Email { get; set; } = string.Empty;

        // --- RELACIONES ---

        [Required]
        public int SitioId { get; set; }

        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;

        /// <summary>
        /// Historial de pencas en las que el usuario ha participado o está participando.
        /// </summary>
        public ICollection<Participacion> Participaciones { get; set; } = new List<Participacion>();

        /// <summary>
        /// Notificaciones recibidas por el usuario dentro del sitio.
        /// </summary>
        public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    }
}
