namespace Penca_uy2026.DTOs
{
    /// <summary>
    /// Estructura de datos para la solicitud de registro de un nuevo usuario en un sitio específico.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Nombre completo del usuario.
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Correo electrónico que servirá como identificador único.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña en texto plano (será hasheada por el servidor).
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// ID del sitio (tenant) en el que se está registrando el usuario.
        /// </summary>
        public int SitioId { get; set; }

        /// <summary>
        /// Slug del sitio (tenant) en el que se está registrando el usuario.
        /// </summary>
        public string? Slug { get; set; }
    }
}
