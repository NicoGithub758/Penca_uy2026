using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class SiteUser
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debes ingresar un nombre")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes ingresar un correo electrónico")]
        public string Email { get; set; } = string.Empty;

        // --- RELACIONES ---
        [Required]
        public int SiteId { get; set; }

        [ForeignKey("SiteId")]
        public Site Site { get; set; } = null!; // Para callar warning por propiedad que no acepta nulos.

        public ICollection<Participation> Participations { get; set; } = new List<Participation>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
