using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models.ViewModels
{
    public class CrearSitioViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string NombreSitio { get; set; }

        [Required(ErrorMessage = "El slug es obligatorio")]
        [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "El slug solo puede contener letras minúsculas, números y guiones.")]
        public string Slug { get; set; }

        public IFormFile? Logo { get; set; }

        [Required]
        public string NombreAdmin { get; set; }

        [Required, EmailAddress]
        public string EmailAdmin { get; set; }

        public TipoRegistro TipoRegistro { get; set; }
    }
}
