using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models.ViewModels
{
    public class RegistrarAdminViewModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes confirmar la contraseña.")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}