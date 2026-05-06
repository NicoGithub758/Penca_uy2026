using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.ViewModels
{
    public class CrearSitioViewModel
    {
        // Datos del Sitio
        [Required(ErrorMessage = "El nombre del sitio es obligatorio")]
        public string NombreSitio { get; set; } = string.Empty;

        [Required(ErrorMessage = "La URL es necesaria (ej: empresa.tupenca.uy)")]
        public string UrlSitio { get; set; } = string.Empty;

        // Datos del Admin del Sitio
        [Required(ErrorMessage = "El nombre del administrador es obligatorio")]
        public string NombreAdmin { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email del administrador es obligatorio")]
        [EmailAddress]
        public string EmailAdmin { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
        public string PasswordAdmin { get; set; } = string.Empty;
    }
}