using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Representa un sitio o instancia de la plataforma tupenca.uy.
    /// Cada sitio puede tener sus propias pencas (instancias), usuarios y configuración.
    /// </summary>
    public class Sitio
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del sitio es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(7)]
        public string? ColorPrincipal { get; set; }

        /// <summary>
        /// Tipo de registro permitido en el sitio.
        /// </summary>
        public TipoRegistro TipoRegistro { get; set; } = TipoRegistro.Abierta;

        public bool Activo { get; set; } = true;

        // --- RELACIONES ---

        /// <summary>
        /// Colección de pencas que han sido instanciadas en este sitio.
        /// </summary>
        public ICollection<PencaInstancia> PencaInstancias { get; set; } = new List<PencaInstancia>();

        /// <summary>
        /// Colección de usuarios registrados específicamente en este sitio.
        /// </summary>
        public ICollection<UsuarioSitio> Usuarios { get; set; } = new List<UsuarioSitio>();

        /// <summary>
        /// Invitaciones enviadas para unirse a este sitio.
        /// </summary>
        public ICollection<Invitacion> Invitaciones { get; set; } = new List<Invitacion>();

        /// <summary>
        /// Solicitudes de acceso pendientes de aprobación por el administrador del sitio.
        /// </summary>
        public ICollection<SolicitudIngreso> SolicitudesIngreso { get; set; } = new List<SolicitudIngreso>();
    }

    public enum TipoRegistro
    {
        Abierta = 0,
        AbiertaConAutorizacion = 1,
        SoloConInvitacion = 2,
        Cerrada = 3
    }
}
