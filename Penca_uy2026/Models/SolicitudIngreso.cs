using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public enum EstadoSolicitud { Pendiente, Aprobada, Rechazada }

    /// <summary>
    /// Representa la solicitud de un usuario externo para unirse a un sitio con autorización.
    /// </summary>
    public class SolicitudIngreso : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

        /// <summary>
        /// Booleano para almacenar si esta solicitud se generó a partir de un link de invitación o desde un registro con autorización.
        /// </summary>
        public bool FuePorInvitacion {  get; set; } = false;

        /// <summary>
        /// Campo temporal de password, para que el usuario haga su solicitud indicando su contraseña, y esta quede guardada para crear posteriormente su usuario.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        // --- RELACIONES ---
        
        [Required]
        public int SitioId { get; set; }

        
        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;
    }
}
