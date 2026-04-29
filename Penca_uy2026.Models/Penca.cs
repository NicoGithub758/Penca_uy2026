using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models
{
    public class Penca
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe indicar un nombre")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // --- RELACIONES ---

        public ICollection<Event> Events { get; set; } = new List<Event>();

        public ICollection<PencaInstance> Instances { get; set; } = new List<PencaInstance>();
    }
}