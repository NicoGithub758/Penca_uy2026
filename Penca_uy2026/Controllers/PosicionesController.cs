using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Jugador")]
    public class PosicionesController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly ILogger<PosicionesController> _logger;

        public PosicionesController(MyDbContext context, ILogger<PosicionesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la tabla de posiciones completa de una penca instancia.
        /// GET /api/posiciones/{pencaInstanciaId}
        /// </summary>
        [HttpGet("{pencaInstanciaId}")]
        public async Task<IActionResult> ObtenerTablaPosiciones(int pencaInstanciaId)
        {
            try
            {
                // Verificar que la penca existe
                var pencaInstancia = await _context.PencaInstancias
                    .FirstOrDefaultAsync(p => p.Id == pencaInstanciaId);

                if (pencaInstancia == null)
                    return NotFound(new { mensaje = "Penca no encontrada" });

                // Obtener participaciones ordenadas por puntos
                var participaciones = await _context.Participaciones
                    .Include(p => p.UsuarioSitio)
                    .Where(p => p.PencaInstanciaId == pencaInstanciaId && p.EstaPagado == true)
                    .OrderByDescending(p => p.PuntajeTotal)
                    .ThenBy(p => p.UsuarioSitio.Nombre)
                    .ToListAsync();

                if (!participaciones.Any())
                    return Ok(new List<PosicionDTO>()); // Lista vacía si no hay participantes

                var usuarioActualId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Construir tabla con posiciones
                var tabla = new List<PosicionDTO>();
                int posicion = 1;
                int? puntosAnteriores = null;

                for (int i = 0; i < participaciones.Count; i++)
                {
                    var p = participaciones[i];

                    // Actualizar posición si los puntos cambiaron
                    if (puntosAnteriores.HasValue && p.PuntajeTotal < puntosAnteriores.Value)
                    {
                        posicion = i + 1;
                    }

                    tabla.Add(new PosicionDTO
                    {
                        Posicion = posicion,
                        UsuarioNombre = p.UsuarioSitio.Nombre,
                        Puntos = p.PuntajeTotal,
                        EsUsuarioActual = p.UsuarioSitioId == usuarioActualId
                    });

                    puntosAnteriores = p.PuntajeTotal;
                }

                _logger.LogInformation($"[Posiciones] Tabla generada para penca {pencaInstanciaId}: {tabla.Count} participantes");

                return Ok(tabla);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Posiciones] Error al obtener tabla: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error al obtener la tabla de posiciones" });
            }
        }

        /// <summary>
        /// Obtiene la posición y puntos del usuario actual en una penca.
        /// GET /api/posiciones/{pencaInstanciaId}/mi-posicion
        /// </summary>
        [HttpGet("{pencaInstanciaId}/mi-posicion")]
        public async Task<IActionResult> ObtenerMiPosicion(int pencaInstanciaId)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var miParticipacion = await _context.Participaciones
                    .FirstOrDefaultAsync(p => p.PencaInstanciaId == pencaInstanciaId
                                            && p.UsuarioSitioId == usuarioId
                                            && p.EstaPagado == true);

                if (miParticipacion == null)
                    return NotFound(new { mensaje = "No estás participando en esta penca" });

                // Contar cuántos tienen más puntos (para calcular posición)
                var posicion = await _context.Participaciones
                    .CountAsync(p => p.PencaInstanciaId == pencaInstanciaId
                                  && p.EstaPagado == true
                                  && p.PuntajeTotal > miParticipacion.PuntajeTotal) + 1;

                // Total de participantes
                var totalParticipantes = await _context.Participaciones
                    .CountAsync(p => p.PencaInstanciaId == pencaInstanciaId && p.EstaPagado == true);

                return Ok(new MiPosicionDTO
                {
                    Posicion = posicion,
                    Puntos = miParticipacion.PuntajeTotal,
                    TotalParticipantes = totalParticipantes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Posiciones] Error al obtener mi posición: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error al obtener tu posición" });
            }
        }

        /// <summary>
        /// Obtiene el top N de la tabla de posiciones.
        /// GET /api/posiciones/{pencaInstanciaId}/top/{cantidad}
        /// </summary>
        [HttpGet("{pencaInstanciaId}/top/{cantidad}")]
        public async Task<IActionResult> ObtenerTopPosiciones(int pencaInstanciaId, int cantidad = 10)
        {
            try
            {
                if (cantidad < 1 || cantidad > 100)
                    return BadRequest(new { mensaje = "La cantidad debe estar entre 1 y 100" });

                var participaciones = await _context.Participaciones
                    .Include(p => p.UsuarioSitio)
                    .Where(p => p.PencaInstanciaId == pencaInstanciaId && p.EstaPagado == true)
                    .OrderByDescending(p => p.PuntajeTotal)
                    .ThenBy(p => p.UsuarioSitio.Nombre)
                    .Take(cantidad)
                    .ToListAsync();

                var usuarioActualId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var tabla = new List<PosicionDTO>();
                int posicion = 1;
                int? puntosAnteriores = null;

                for (int i = 0; i < participaciones.Count; i++)
                {
                    var p = participaciones[i];

                    if (puntosAnteriores.HasValue && p.PuntajeTotal < puntosAnteriores.Value)
                    {
                        posicion = i + 1;
                    }

                    tabla.Add(new PosicionDTO
                    {
                        Posicion = posicion,
                        UsuarioNombre = p.UsuarioSitio.Nombre,
                        Puntos = p.PuntajeTotal,
                        EsUsuarioActual = p.UsuarioSitioId == usuarioActualId
                    });

                    puntosAnteriores = p.PuntajeTotal;
                }

                return Ok(tabla);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Posiciones] Error al obtener top {cantidad}: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error al obtener el top de posiciones" });
            }
        }
    }
}
