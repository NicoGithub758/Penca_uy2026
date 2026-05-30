namespace Penca_uy2026.DTOs
{
    /// <summary>
    /// DTO para el proceso de actualizar contraseña desde el frontend de los usuarios finales.
    /// </summary>
    public class UpdatePasswordDTO
    {
        // Es opcional puesto que usuarios registrados con Google no tendrán contraseña antigua la primera vez que actualicen su password.
        public string? OldPassword { get; set; }

        public string NewPassword { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // Es necesario por seguridad.
    }
}
