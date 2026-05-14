using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class UsuarioSitio : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debes ingresar un nombre")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes ingresar un correo electrónico")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Email { get; set; } = string.Empty;

        // Tiene que poder ser nullable para los casos de usuarios que sólo se loguean mediante proveedor externo.
        public string? PasswordHash { get; set; }

        [Required]
        public RolUsuarioSitio Rol { get; set; } = RolUsuarioSitio.Jugador;

        public bool Activo { get; set; } = true;

        public string? Auth0Id { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public string? FcmToken { get; set; }

        // --- RELACIONES ---

        [Required]
        public int SitioId { get; set; }

        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;

        public ICollection<Participacion> Participaciones { get; set; } = new List<Participacion>();
        public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    }

    public enum RolUsuarioSitio
    {
        Jugador = 0,
        AdminSitio = 1
    }
}