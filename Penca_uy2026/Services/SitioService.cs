using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;

namespace Penca_uy2026.Services
{
    /// <summary>
    /// Servicio de lógica de negocio para la gestión de Sitios.
    /// Sigue el patrón N-Tier separando el acceso a datos del controlador.
    /// </summary>
    public class SitioService
    {
        private readonly MyDbContext _context;

        public SitioService(MyDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la configuración pública de un sitio buscando por su slug.
        /// </summary>
        /// <param name="slug">El identificador único en la URL.</param>
        /// <returns>SitioConfigDTO si existe y está activo, null en caso contrario.</returns>
        public async Task<SitioConfigDTO?> ObtenerConfiguracionPorSlugAsync(string slug)
        {
            var slugNormalizado = slug.Trim().ToLowerInvariant();

            return await _context.Sitios
                .Where(s => s.Slug == slugNormalizado && s.Activo)
                .Select(s => new SitioConfigDTO
                {
                    Nombre = s.Nombre,
                    Slug = s.Slug,
                    TipoRegistro = s.TipoRegistro,
                    LogoUrl = s.LogoUrl,
                    ColorPrincipal = s.ColorPrincipal
                })
                .FirstOrDefaultAsync();
        }
    }
}
