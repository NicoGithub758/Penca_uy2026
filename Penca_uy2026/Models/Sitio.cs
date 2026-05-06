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

        // NUEVO: Para identificar el sitio por URL (ej: "empresa.tupenca.uy" o "localhost:5001")
        [Required]
        [MaxLength(255)]
        public string Url { get; set; } = string.Empty;

        // --- RELACIONES ---
        // (Tus colecciones actuales se mantienen igual)
        public ICollection<PencaInstancia> PencaInstancias { get; set; } = new List<PencaInstancia>();
        public ICollection<UsuarioSitio> Usuarios { get; set; } = new List<UsuarioSitio>();
        public ICollection<Invitacion> Invitaciones { get; set; } = new List<Invitacion>();
        public ICollection<SolicitudIngreso> SolicitudesIngreso { get; set; } = new List<SolicitudIngreso>();
    }
}