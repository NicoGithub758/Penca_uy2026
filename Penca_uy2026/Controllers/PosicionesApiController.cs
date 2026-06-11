using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Services;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/pencas-sitio/{slug}/posiciones")]
    [Authorize(Roles = "Jugador")]
    public class PosicionesApiController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly PosicionesService _posicionesService;
        private readonly UsuarioAuthService _usuarioAuthService;

        public PosicionesApiController(MyDbContext context, PosicionesService posicionesService, UsuarioAuthService usuarioAuthService)
        {
            _context = context;
            _posicionesService = posicionesService;
            _usuarioAuthService = usuarioAuthService;
        }

        /// <summary>
        /// Obtiene la tabla de posiciones de una penca instancia aislando los datos por sitio.
        /// GET /api/pencas-sitio/{slug}/posiciones/{pencaInstanciaId}
        /// </summary>
        [HttpGet("{pencaInstanciaId}")]
        public async Task<IActionResult> ObtenerTablaPosicionesSegura(string slug, int pencaInstanciaId)
        {
            try
            {
                var sitioIdClaim = User.FindFirst("sitioId")?.Value;
                if (string.IsNullOrEmpty(sitioIdClaim) || !int.TryParse(sitioIdClaim, out int tokenSitioId))
                {
                    return Unauthorized(new { mensaje = "Token inválido o sin sitio asociado." });
                }

                // Obtener el ID del usuario logueado
                var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out int usuarioActualId))
                {
                    return Unauthorized(new { mensaje = "Token inválido." });
                }

                // Validar seguridad del sitio y del usuario
                bool tieneAcceso = await _usuarioAuthService.ValidarAccesoSitioYUsuarioAsync(usuarioActualId, tokenSitioId, slug);
                if (!tieneAcceso)
                {
                    return Forbid();
                }

                // Delegar la lógica de negocio al servicio (Fat Service / Thin Controller)
                var tabla = await _posicionesService.ObtenerTablaPosicionesAsync(pencaInstanciaId, usuarioActualId);

                return Ok(tabla);
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error interno del servidor al calcular las posiciones." });
            }
        }
    }
}
