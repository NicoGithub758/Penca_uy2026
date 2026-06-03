using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/mobile/pencas")]
    [Authorize(Roles = "Jugador")]
    public class PencasMobileController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly ILogger<PencasMobileController> _logger;

        public PencasMobileController(MyDbContext context, ILogger<PencasMobileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lista todas las pencas instancia del sitio del usuario (sacado del JWT).
        /// Incluye info de si el usuario participa o no en cada una.
        /// GET /api/mobile/pencas
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerPencasDelSitio()
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // El filtro multi-tenant ya filtra PencaInstancias por el sitio del JWT.
                // No hace falta pasar slug ni sitioId manualmente.
                var pencas = await _context.PencaInstancias
                    .Include(pi => pi.Penca)
                        .ThenInclude(p => p.Deporte)
                    .Include(pi => pi.Participaciones)
                    .Select(pi => new PencaInstanciaMobileDTO
                    {
                        PencaInstanciaId = pi.Id,
                        PencaId = pi.Penca.Id,
                        NombrePenca = pi.Penca.Nombre,
                        Deporte = pi.Penca.Deporte != null ? pi.Penca.Deporte.Nombre : "",
                        CantidadEquipos = pi.Penca.CantidadEquipos,
                        Costo = pi.Costo,
                        Finalizada = pi.Penca.Finalizada,

                        // Datos de la participación del usuario actual (si existe)
                        Participa = pi.Participaciones.Any(parti => parti.UsuarioSitioId == usuarioId),
                        ParticipacionId = pi.Participaciones
                            .Where(parti => parti.UsuarioSitioId == usuarioId)
                            .Select(parti => (int?)parti.Id)
                            .FirstOrDefault(),
                        EstaPagado = pi.Participaciones
                            .Where(parti => parti.UsuarioSitioId == usuarioId)
                            .Select(parti => parti.EstaPagado)
                            .FirstOrDefault(),
                        PuntajeTotal = pi.Participaciones
                            .Where(parti => parti.UsuarioSitioId == usuarioId)
                            .Select(parti => parti.PuntajeTotal)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                _logger.LogInformation($"[PencasMobile] {pencas.Count} pencas devueltas para usuario {usuarioId}");

                return Ok(pencas);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PencasMobile] Error: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error al obtener las pencas" });
            }
        }
    }
}
