using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class Site
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del sitio es obligatorio")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // --- Relaciones ---

        public ICollection<PencaInstance> PencaInstances { get; set; } = new List<PencaInstance>();

        public ICollection<SiteUser> SiteUsers { get; set; } = new List<SiteUser>();

        public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

        public ICollection<AccessRequest> AccessRequests { get; set; } = new List<AccessRequest>();
    }
}
