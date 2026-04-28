using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models
{
    public class Site
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del sitio es obligatorio")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Se agregarán más atributos y las relaciones..
    }
}
