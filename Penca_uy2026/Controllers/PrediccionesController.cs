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
    [Route("api/[controller]")]
    public class PrediccionesController : ControllerBase
    {
        private readonly MyDbContext _context;

        public PrediccionesController(MyDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("partidos")]
        public async Task<IActionResult> Partidos([FromQuery] int idParticipacion)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var sitioId = int.Parse(User.FindFirstValue("sitioId"));

            if (!await ValidarParticipacion(usuarioId, sitioId, idParticipacion))
                return BadRequest("Error participacion invalida.");

            var partidos = await _context.Partidos
                .Include(p => p.Local)
                .Include(p => p.Visitante)
                .Where(pe => pe.PencaId == _context.Participaciones
                    .Where(p => p.Id == idParticipacion)
                    .Select(p => p.PencaInstancia.PencaId)
                    .FirstOrDefault())
                .OrderBy(partido => partido.Jugado)
                .Select(partido => new
                {
                    partido.Id,
                    partido.Local,
                    partido.Visitante,
                    partido.Fecha,
                    partido.GolesLocal,
                    partido.GolesVisitante,
                    partido.Jugado,

                    Prediccion = _context.Predicciones
                        .Where(p => p.ParticipacionId == idParticipacion)
                        .Where(p => p.PartidoId == partido.Id)
                        .Select(p => new
                        {
                            p.Id,
                            p.GolesEquipoLocal,
                            p.GolesEquipoVisitante,
                            p.PuntosObtenidos
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(partidos);
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> RealizarPrediccion([FromBody] PrediccionDTO prediccionDTO)
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var sitioId = int.Parse(User.FindFirstValue("sitioId"));

            if (_context.Partidos.Where(p => p.Id == prediccionDTO.PartidoId)
                .Select(p => p.Fecha).FirstOrDefault() <= DateTime.UtcNow.AddHours(-3))
            {
                return BadRequest("Partido ya empezado");
            }

            if (!await ValidarParticipacion(usuarioId, sitioId, prediccionDTO.ParticipacionId))
                return BadRequest("Error participacion invalida.");

            if (await PartidoYaJugado(prediccionDTO.PartidoId))
                return BadRequest("No se puede predecir un partido ya jugado.");

            if (!ValidarFormatoResultado(prediccionDTO))
                return BadRequest("Error en el formato de resultados.");

            var prediccionExistente = await _context.Predicciones
                .FirstOrDefaultAsync(p => p.PartidoId == prediccionDTO.PartidoId
                    && p.ParticipacionId == prediccionDTO.ParticipacionId
                    && p.SitioId == sitioId);

            if (prediccionExistente != null)
            {
                if (await ValidarPrediccionExistente(usuarioId, prediccionDTO))
                {
                    prediccionExistente.GolesEquipoLocal = prediccionDTO.GolesEquipoLocal;
                    prediccionExistente.GolesEquipoVisitante = prediccionDTO.GolesEquipoVisitante;
                }
                else
                {
                    return BadRequest("Error: datos invalidos.");
                }
            }
            else
            {
                if (await ValidarNuevaPrediccion(prediccionDTO))
                {
                    var nuevaPrediccion = new Prediccion
                    {
                        GolesEquipoLocal = prediccionDTO.GolesEquipoLocal,
                        GolesEquipoVisitante = prediccionDTO.GolesEquipoVisitante,
                        PuntosObtenidos = 0,
                        ParticipacionId = prediccionDTO.ParticipacionId,
                        PartidoId = prediccionDTO.PartidoId,
                        SitioId = sitioId
                    };
                    _context.Predicciones.Add(nuevaPrediccion);
                }
                else
                {
                    return BadRequest("Error: datos invalidos.");
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [Authorize]
        [HttpGet("resultados")]
        public async Task<IActionResult> ResultadosPredicciones([FromQuery] int partidoId)
        {
            var partido = await _context.Partidos.FirstOrDefaultAsync(p => p.Id == partidoId);

            if (partido == null)
                return NotFound();

            var prediccionesPartido = await _context.Predicciones
                .Where(p => p.PartidoId == partidoId)
                .ToListAsync();

            var totalGeneral = prediccionesPartido.Count;

            if (totalGeneral == 0)
            {
                return Ok(new
                {
                    TotalPredicciones = 0,
                    CantidadExacto = 0,
                    PorcentajeExacto = 0,
                    CantidadGanadorDiferencia = 0,
                    PorcentajeGanadorDiferencia = 0,
                    CantidadTendencia = 0,
                    PorcentajeTendencia = 0,
                    CantidadPerdedores = 0,
                    PorcentajePerdedores = 0
                });
            }

            if (!partido.Jugado || !partido.GolesLocal.HasValue || !partido.GolesVisitante.HasValue)
            {
                return Ok(new
                {
                    TotalPredicciones = totalGeneral,
                    CantidadExacto = 0,
                    PorcentajeExacto = 0,
                    CantidadGanadorDiferencia = 0,
                    PorcentajeGanadorDiferencia = 0,
                    CantidadTendencia = 0,
                    PorcentajeTendencia = 0,
                    CantidadPerdedores = totalGeneral,
                    PorcentajePerdedores = 100
                });
            }

            var cantidadExacto = prediccionesPartido.Count(p => EsResultadoExacto(partido, p));
            var cantidadGanadorDiferencia = prediccionesPartido.Count(p => AcertoGanadorDiferencia(partido, p));
            var cantidadTendencia = prediccionesPartido.Count(p =>
                !EsResultadoExacto(partido, p) &&
                !AcertoGanadorDiferencia(partido, p) &&
                AcertoGanadorOEmpate(partido, p));
            var cantidadPerdedores = totalGeneral - cantidadExacto - cantidadGanadorDiferencia - cantidadTendencia;

            var resultado = new
            {
                TotalPredicciones = totalGeneral,

                CantidadExacto = cantidadExacto,
                PorcentajeExacto = Math.Round(cantidadExacto * 100.0 / totalGeneral, 1),

                CantidadGanadorDiferencia = cantidadGanadorDiferencia,
                PorcentajeGanadorDiferencia = Math.Round(cantidadGanadorDiferencia * 100.0 / totalGeneral, 1),

                CantidadTendencia = cantidadTendencia,
                PorcentajeTendencia = Math.Round(cantidadTendencia * 100.0 / totalGeneral, 1),

                CantidadPerdedores = cantidadPerdedores,
                PorcentajePerdedores = Math.Round(cantidadPerdedores * 100.0 / totalGeneral, 1)
            };

            return Ok(resultado);
        }

        [Authorize]
        [HttpGet("tendencia")]
        public async Task<IActionResult> GetTendenciaPartido([FromQuery] int partidoId)
        {
            try
            {
                var predicciones = await _context.Predicciones
                    .Where(p => p.PartidoId == partidoId)
                    .Select(p => new
                    {
                        GolesL = p.GolesEquipoLocal,
                        GolesV = p.GolesEquipoVisitante
                    })
                    .ToListAsync();

                var total = predicciones.Count;

                if (total == 0)
                {
                    return Ok(new
                    {
                        totalPredicciones = 0,
                        porcentajes = new { local = 34, empate = 33, visitante = 33 }
                    });
                }

                var cantidadLocal = predicciones.Count(p => p.GolesL > p.GolesV);
                var cantidadVisitante = predicciones.Count(p => p.GolesV > p.GolesL);
                var cantidadEmpate = predicciones.Count(p => p.GolesL == p.GolesV);

                var resultado = new
                {
                    totalPredicciones = total,
                    porcentajes = new
                    {
                        local = (int)Math.Round((double)cantidadLocal / total * 100),
                        empate = (int)Math.Round((double)cantidadEmpate / total * 100),
                        visitante = (int)Math.Round((double)cantidadVisitante / total * 100)
                    }
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al calcular tendencia: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno al calcular las tendencias." });
            }
        }

        private async Task<bool> ValidarParticipacion(int userId, int sitioId, int participacionId)
        {
            return await _context.Participaciones
                .AnyAsync(p => p.UsuarioSitioId == userId && p.SitioId == sitioId && p.Id == participacionId);
        }

        private async Task<bool> ValidarPrediccionExistente(int idUsuario, PrediccionDTO dto)
        {
            return await _context.Predicciones
                .AnyAsync(p => p.Id == dto.Id && p.Participacion.UsuarioSitioId == idUsuario);
        }

        private async Task<bool> ValidarNuevaPrediccion(PrediccionDTO dto)
        {
            return await _context.Participaciones
                .AnyAsync(p => p.Id == dto.ParticipacionId
                    && p.PencaInstancia.Penca.Partidos.Any(partido => partido.Id == dto.PartidoId));
        }

        private bool ValidarFormatoResultado(PrediccionDTO dto)
        {
            return dto.GolesEquipoLocal >= 0 && dto.GolesEquipoVisitante >= 0;
        }

        private async Task<bool> PartidoYaJugado(int partidoId)
        {
            var partido = await _context.Partidos.FindAsync(partidoId);
            return partido?.Jugado == true;
        }

        private static bool EsResultadoExacto(Partido partido, Prediccion prediccion)
        {
            return prediccion.GolesEquipoLocal == (partido.GolesLocal ?? 0) &&
                prediccion.GolesEquipoVisitante == (partido.GolesVisitante ?? 0);
        }

        private static bool AcertoGanadorDiferencia(Partido partido, Prediccion prediccion)
        {
            var diferenciaReal = DiferenciaGoles(partido.GolesLocal ?? 0, partido.GolesVisitante ?? 0);
            var diferenciaPredicha = DiferenciaGoles(prediccion.GolesEquipoLocal, prediccion.GolesEquipoVisitante);

            return !EsResultadoExacto(partido, prediccion) &&
                diferenciaReal != 0 &&
                diferenciaReal == diferenciaPredicha;
        }

        private static bool AcertoGanadorOEmpate(Partido partido, Prediccion prediccion)
        {
            var diferenciaReal = DiferenciaGoles(partido.GolesLocal ?? 0, partido.GolesVisitante ?? 0);
            var diferenciaPredicha = DiferenciaGoles(prediccion.GolesEquipoLocal, prediccion.GolesEquipoVisitante);

            return (diferenciaReal > 0 && diferenciaPredicha > 0) ||
                (diferenciaReal < 0 && diferenciaPredicha < 0) ||
                (diferenciaReal == 0 && diferenciaPredicha == 0);
        }

        private static int DiferenciaGoles(int golesLocal, int golesVisitante)
        {
            return golesLocal - golesVisitante;
        }
    }
}
