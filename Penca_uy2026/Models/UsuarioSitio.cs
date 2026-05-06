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

        // NUEVO: Para poder loguearse al sitio
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // NUEVO: Para saber si este usuario es el que manda en el Sitio
        public bool EsAdminSitio { get; set; } = false;

        // --- RELACIONES ---

        [Required]
        public int SitioId { get; set; }

        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;

        public ICollection<Participacion> Participaciones { get; set; } = new List<Participacion>();
        public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();
    }
}