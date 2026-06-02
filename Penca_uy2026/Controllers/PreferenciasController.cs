using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/mobile/preferencias")]
    [Authorize(Roles = "Jugador")]
    public class PreferenciasController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly ILogger<PreferenciasController> _logger;

        public PreferenciasController(MyDbContext context, ILogger<PreferenciasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene las preferencias de notificaciones del usuario actual.
        /// Si no existen, las crea con valores default (todas en true).
        /// GET /api/mobile/preferencias/notificaciones
        /// </summary>
        [HttpGet("notificaciones")]
        public async Task<IActionResult> GetPreferencias()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var prefs = await _context.PreferenciasNotificacion
                .FirstOrDefaultAsync(p => p.UsuarioSitioId == usuarioId.Value);

            // Si el usuario nunca configuro preferencias, crear con defaults
            if (prefs == null)
            {
                var sitioIdClaim = User.FindFirst("sitioId")?.Value;
                if (!int.TryParse(sitioIdClaim, out int sitioId))
                    return BadRequest(new { mensaje = "No se pudo determinar el sitio." });

                prefs = new PreferenciaNotificacion
                {
                    UsuarioSitioId = usuarioId.Value,
                    SitioId = sitioId
                    // El resto queda con sus valores default = true
                };
                _context.PreferenciasNotificacion.Add(prefs);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[Preferencias] Creadas defaults para usuario {usuarioId}");
            }

            return Ok(new PreferenciasNotificacionDTO
            {
                RecibirResultados = prefs.RecibirResultados,
                RecibirPartidos = prefs.RecibirPartidos,
                RecibirGenerales = prefs.RecibirGenerales,
                RecibirRanking = prefs.RecibirRanking
            });
        }

        /// <summary>
        /// Actualiza las preferencias de notificaciones del usuario actual.
        /// PUT /api/mobile/preferencias/notificaciones
        /// </summary>
        [HttpPut("notificaciones")]
        public async Task<IActionResult> UpdatePreferencias([FromBody] PreferenciasNotificacionDTO request)
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Unauthorized();

            var prefs = await _context.PreferenciasNotificacion
                .FirstOrDefaultAsync(p => p.UsuarioSitioId == usuarioId.Value);

            // Si no existen, las creamos antes de actualizar
            if (prefs == null)
            {
                var sitioIdClaim = User.FindFirst("sitioId")?.Value;
                if (!int.TryParse(sitioIdClaim, out int sitioId))
                    return BadRequest(new { mensaje = "No se pudo determinar el sitio." });

                prefs = new PreferenciaNotificacion
                {
                    UsuarioSitioId = usuarioId.Value,
                    SitioId = sitioId
                };
                _context.PreferenciasNotificacion.Add(prefs);
            }

            // Actualizar los 4 campos con los valores recibidos
            prefs.RecibirResultados = request.RecibirResultados;
            prefs.RecibirPartidos = request.RecibirPartidos;
            prefs.RecibirGenerales = request.RecibirGenerales;
            prefs.RecibirRanking = request.RecibirRanking;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"[Preferencias] Actualizadas para usuario {usuarioId}");

            return Ok(new { mensaje = "Preferencias actualizadas correctamente." });
        }

        // -----------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------

        private int? ObtenerUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }
    }
}
