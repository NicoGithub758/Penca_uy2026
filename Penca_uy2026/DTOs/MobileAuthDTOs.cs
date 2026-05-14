namespace Penca_uy2026.DTOs
{
    /// <summary>
    /// Request para login social desde la app móvil.
    /// </summary>
    public class SocialLoginRequest
    {
        /// <summary>
        /// Token de Auth0 obtenido después del login con Google.
        /// </summary>
        public string Auth0Token { get; set; } = string.Empty;

        /// <summary>
        /// ID del sitio al que el usuario quiere acceder.
        /// </summary>
        public int SitioId { get; set; }

        /// <summary>
        /// Slug del sitio al que el usuario quiere acceder.
        /// </summary>
        public string? Slug { get; set; }
    }

    /// <summary>
    /// Respuesta al login social exitoso.
    /// </summary>
    public class SocialLoginResponse
    {
        public string Jwt { get; set; } = string.Empty;
        public int UsuarioSitioId { get; set; }
        public int SitioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
    }

    /// <summary>
    /// Request para registrar/actualizar el token FCM del dispositivo.
    /// </summary>
    public class FcmTokenRequest
    {
        public string FcmToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para mostrar un sitio en la lista de la app móvil.
    /// </summary>
    public class SitioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? LogoUrl { get; set; }
        public string? ColorPrincipal { get; set; }
        public string TipoRegistro { get; set; } = string.Empty;
    }
}
