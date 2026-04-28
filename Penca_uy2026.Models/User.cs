using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debes ingresar un nombre")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes ingresar un correo electrónico")]
        public string Email { get; set; } = string.Empty;

        // Se agregarán más atributos y las relaciones...
    }
}
