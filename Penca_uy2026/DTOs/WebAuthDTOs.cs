namespace Penca_uy2026.DTOs
{
    /// <summary>
    /// Request para login social desde la aplicación web.
    /// </summary>
    public class WebSocialLoginRequest
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
    /// Respuesta al login social exitoso para la web.
    /// </summary>
    public class WebSocialLoginResponse
    {
        public string Jwt { get; set; } = string.Empty;
        public int UsuarioSitioId { get; set; }
        public int SitioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
        public bool TienePassword { get; set; }
        public bool TieneGoogle { get; set; }
    }
}
