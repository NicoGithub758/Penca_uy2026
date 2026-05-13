// ... tus usings actuales
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class Sitio
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del sitio es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Url { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Slug { get; set; } = string.Empty;
        [Required]
        public TipoRegistro TipoRegistro { get; set; }

        public bool Activo { get; set; } = true;

        // --- RELACIONES ---
        public ICollection<PencaInstancia> PencaInstancias { get; set; } = new List<PencaInstancia>();
        public ICollection<UsuarioSitio> Usuarios { get; set; } = new List<UsuarioSitio>();
        public ICollection<Invitacion> Invitaciones { get; set; } = new List<Invitacion>();
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