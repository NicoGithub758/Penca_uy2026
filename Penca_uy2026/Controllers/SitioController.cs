using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;

namespace Penca_uy2026.Controllers
{
    /// <summary>
    /// Controlador para gestionar la información pública de los sitios (tenants).
    /// Permite que el frontend valide la existencia de un sitio antes de procesar login/registro.
    /// </summary>
    [ApiController]
    [Route("api/sitios")]
    public class SitioController : ControllerBase
    {
        private readonly MyDbContext _context;

        public SitioController(MyDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Verifica si un slug pertenece a un sitio registrado y activo y devuelve su configuración básica.
        /// </summary>
        /// <param name="slug">El identificador único del sitio en la URL.</param>
        /// <returns>Información del sitio si es válido, 404 Not Found si no existe.</returns>
        [HttpGet("validar/{slug}")]
        public async Task<IActionResult> ValidarSlug(string slug)
        {
            // Buscamos el sitio por su slug.
            var sitio = await _context.Sitios
                .Where(s => s.Slug == slug && s.Activo)
                .Select(s => new {
                    s.Nombre,
                    s.Slug,
                    s.TipoRegistro,
                    s.LogoUrl,
                    s.ColorPrincipal
                })
                .FirstOrDefaultAsync();

            if (sitio == null)
            {
                // Si no existe, devolvemos un 404 indicando que el "tenant" no es válido.
                return NotFound(new { mensaje = "El sitio especificado no existe o no está disponible." });
            }

            // Devolvemos la configuración del sitio para que el frontend se adapte.
            return Ok(sitio);
        }
    }
}
