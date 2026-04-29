using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class PencaInstance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")] // Obligatorio en EF Core para evitar pérdida de precisión en dinero
        public decimal ComissionPercentage { get; set; } = 0;

        // --- RELACIONES ---

        [Required]
        public int PencaId { get; set; }
        [ForeignKey("PencaId")]
        public Penca Penca { get; set; } = null!; // Propiedad de navegación

        [Required]
        public int SiteId { get; set; }
        [ForeignKey("SiteId")]
        public Site Site { get; set; } = null!;

        public ICollection<Participation> Participations { get; set; } = new List<Participation>();
    }
}