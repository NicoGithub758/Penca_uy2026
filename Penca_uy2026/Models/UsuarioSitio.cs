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

        // Login con Google/Auth0
        [MaxLength(200)]
        public string? Auth0Id { get; set; }

        // Login interno
        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        // Notificaciones push
        [MaxLength(500)]
        public string? FcmToken { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public RolUsuarioSitio Rol { get; set; } = RolUsuarioSitio.Jugador;

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

    public enum RolUsuarioSitio
    {
        Jugador = 0,
        AdminSitio = 1
    }
}
