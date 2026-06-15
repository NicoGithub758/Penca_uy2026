using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.DTOs;
using Penca_uy2026.Services;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/preferencias/{slug}")]
    [Authorize(Roles = "Jugador")]
    public class PreferenciasWebController : ControllerBase
    {
        private readonly PreferenciasService _preferenciasService;

        public PreferenciasWebController(PreferenciasService preferenciasService)
        {
            _preferenciasService = preferenciasService;
        }

        [HttpGet("notificaciones")]
        public async Task<IActionResult> GetPreferencias(string slug)
        {
            var usuarioId = ObtenerUsuarioId();
            var jwtSitioId = ObtenerJwtSitioId();

            if (usuarioId == null || jwtSitioId == null) return Unauthorized();

            var (prefs, errorMessage) = await _preferenciasService.GetPreferenciasAsync(usuarioId.Value, jwtSitioId.Value, slug);

            if (errorMessage != null)
            {
                if (errorMessage == "FORBIDDEN") return Forbid();
                if (errorMessage == "NOT_FOUND") return NotFound("Sitio no encontrado");
                return BadRequest();
            }

            return Ok(prefs);
        }

        [HttpPut("notificaciones")]
        public async Task<IActionResult> UpdatePreferencias(string slug, [FromBody] PreferenciasNotificacionDTO request)
        {
            var usuarioId = ObtenerUsuarioId();
            var jwtSitioId = ObtenerJwtSitioId();

            if (usuarioId == null || jwtSitioId == null) return Unauthorized();

            var (success, errorMessage) = await _preferenciasService.UpdatePreferenciasAsync(usuarioId.Value, jwtSitioId.Value, slug, request);

            if (!success)
            {
                if (errorMessage == "FORBIDDEN") return Forbid();
                if (errorMessage == "NOT_FOUND") return NotFound("Sitio no encontrado");
                return BadRequest(new { mensaje = "No se pudieron actualizar las preferencias." });
            }

            return Ok(new { mensaje = "Preferencias actualizadas correctamente." });
        }

        // Helpers
        private int? ObtenerUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }

        private int? ObtenerJwtSitioId()
        {
            var claim = User.FindFirst("sitioId")?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }
    }
}
