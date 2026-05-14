using Penca_uy2026.Models;

namespace Penca_uy2026.DTOs
{
    /// <summary>
    /// DTO que contiene la configuración pública de un sitio para que el frontend se adapte.
    /// Solo exponemos los datos necesarios para la interfaz de usuario.
    /// </summary>
    public class SitioConfigDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public TipoRegistro TipoRegistro { get; set; }
        public string? LogoUrl { get; set; }
        public string? ColorPrincipal { get; set; }
    }
}
