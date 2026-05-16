using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Data;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    /// <summary>
    /// Controlador para la gestión de sitios. 
    /// Combina acciones de Backoffice (Views) con endpoints de API para el frontend.
    /// </summary>
    [Authorize(Roles = "PlataformaAdmin")]
    public class SitiosController : Controller
    {
        private readonly MyDbContext _context;
        private readonly SitioService _sitioService;

        public SitiosController(MyDbContext context, SitioService sitioService)
        {
            _context = context;
            _sitioService = sitioService;
        }

        // --- ACCIONES DE BACKOFFICE (Razor Views) ---

        public IActionResult Index()
        {
            var sitios = _context.Sitios.ToList();
            return View(sitios);
        }

        // --- ENDPOINTS DE API (Para el Frontend React) ---

        /// <summary>
        /// Verifica si un slug pertenece a un sitio registrado y activo.
        /// Este endpoint es público porque se usa antes de que el usuario se loguee.
        /// </summary>
        [HttpGet("api/sitios/validar/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarSlug(string slug)
        {
            // Delegamos la lógica de negocio al servicio (Arquitectura N-Tier)
            var sitio = await _sitioService.ObtenerConfiguracionPorSlugAsync(slug);

            if (sitio == null)
            {
                return NotFound(new { mensaje = "El sitio especificado no existe o no está disponible." });
            }

            return Ok(sitio);
        }
    }
}
