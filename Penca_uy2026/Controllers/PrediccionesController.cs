using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.AppConfig;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;
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

        // Listado de partidos de una penca con las predicciones del usuario
        
        [Authorize]
        [HttpGet("partidos")]
        public async Task<IActionResult> Partidos([FromQuery] int idParticipacion)
        {
            
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var sitioId = int.Parse(User.FindFirstValue("sitioId"));

            if(! await ValidarParticipacion(usuarioId, sitioId, idParticipacion))
                return BadRequest("Error participacion inválida.");
 

            var partidos = await _context.Partidos
                .Include(p => p.Local)
                .Include(p => p.Visitante)
                .Where(pe => pe.PencaId ==  _context.Participaciones
                                    .Where(p => p.Id == idParticipacion)
                                    .Select(p => p.PencaInstancia.PencaId)
                .FirstOrDefault()).OrderBy(partido => partido.Jugado)
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
                            .FirstOrDefault()}).ToListAsync();

            return Ok(partidos);
        }

        // Valida y persiste predicción realizada por el usuario en una penca que participa
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> RealizarPrediccion([FromBody] PrediccionDTO prediccionDTO){
            
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var sitioId = int.Parse(User.FindFirstValue("sitioId"));
            
            if(_context.Partidos.Where(p=> p.Id == prediccionDTO.PartidoId)
            .Select(p => p.Fecha).FirstOrDefault() <= DateTime.UtcNow){
                return BadRequest("Partido ya empezado");
            }

            if(!await ValidarParticipacion(usuarioId, sitioId, prediccionDTO.ParticipacionId))
                return BadRequest("Error participacion inválida.");
            
            if(await PartidoYaJugado(prediccionDTO.PartidoId))
                return BadRequest("No se puede predecir un partido ya jugado.");
            
            if(!ValidarFormatoResultado(prediccionDTO)){
                return BadRequest("Error en el formato de resultados.");
            }

                        // Buscamos si este usuario/participación ya tiene una predicción asignada a este partido específico
            var prediccionExistente = await _context.Predicciones
                .FirstOrDefaultAsync(p => p.PartidoId == prediccionDTO.PartidoId 
                                    && p.ParticipacionId == prediccionDTO.ParticipacionId 
                                    && p.SitioId == sitioId);
            
            if(prediccionExistente != null){        // Existe prediccion
                if(await ValidarPrediccionExistente(usuarioId, prediccionDTO)){
                    prediccionExistente.GolesEquipoLocal = prediccionDTO.GolesEquipoLocal;
                    prediccionExistente.GolesEquipoVisitante = prediccionDTO.GolesEquipoVisitante;
                }
                else{
                    return BadRequest("Error: datos invalidos.");        
                }
            }else{          // Nueva prediccion
                if(await ValidarNuevaPrediccion(prediccionDTO)){
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
                }else{
                    return BadRequest("Error: datos invalidos.");        
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
            
        }

        [Authorize]
        [HttpGet("resultados")]
        public async Task<IActionResult> ResultadosPredicciones([FromQuery] int partidoId){
            
           var prediccionesPartido = _context.Predicciones
        .Where(p => p.PartidoId == partidoId)
        .ToList();

    int totalGeneral = prediccionesPartido.Count;

    // 2. Si no hay trys todavía, devolvemos un objeto limpio para que no rompa
    if (totalGeneral == 0)
    {
        return Ok(new {
            TotalPredicciones = 0,
            CantidadExacto = 0, PorcentajeExacto = 0,
            CantidadTendencia = 0, PorcentajeTendencia = 0,
            CantidadPerdedores = 0, PorcentajePerdedores = 0
        });
    }

    // 3. Armamos el objeto final mapeando directamente por el puntaje
    var resultado = new
    {
        TotalPredicciones = totalGeneral,

        // Contamos cuántos sacaron 10 puntos y calculamos su %
        CantidadExacto = prediccionesPartido.Count(p => p.PuntosObtenidos == 10),
        PorcentajeExacto = Math.Round(prediccionesPartido.Count(p => p.PuntosObtenidos == 10) * 100.0 / totalGeneral, 1),

        // Contamos cuántos sacaron 5 puntos y calculamos su %
        CantidadTendencia = prediccionesPartido.Count(p => p.PuntosObtenidos == 5),
        PorcentajeTendencia = Math.Round(prediccionesPartido.Count(p => p.PuntosObtenidos == 5) * 100.0 / totalGeneral, 1),

        // Contamos cuántos sacaron 0 puntos y calculamos su %
        CantidadPerdedores = prediccionesPartido.Count(p => p.PuntosObtenidos == 0),
        PorcentajePerdedores = Math.Round(prediccionesPartido.Count(p => p.PuntosObtenidos == 0) * 100.0 / totalGeneral, 1)
    };

    return Ok(resultado);
}       
        [Authorize]
        [HttpGet("tendencia")]
        public async Task<IActionResult> GetTendenciaPartido([FromQuery] int partidoId)
        {
            var id = partidoId;
            try
            {
                // 1. Traemos las predicciones de este partido desde la base de datos
                var predicciones = await _context.Predicciones
                    .Where(p => p.PartidoId == id)
                    .Select(p => new 
                    {
                        GolesL = p.GolesEquipoLocal,
                        GolesV = p.GolesEquipoVisitante
                    })
                    .ToListAsync();

                int total = predicciones.Count;

                // 2. Si nadie apostó todavía, devolvemos una tendencia inicial neutra
                if (total == 0)
                {
                    return Ok(new
                    {
                        totalPredicciones = 0,
                        porcentajes = new { local = 34, empate = 33, visitante = 33 }
                    });
                }

                // 3. Contamos los resultados según los goles arriesgados
                int cantidadLocal = predicciones.Count(p => p.GolesL > p.GolesV);
                int cantidadVisitante = predicciones.Count(p => p.GolesV > p.GolesL);
                int cantidadEmpate = predicciones.Count(p => p.GolesL == p.GolesV);

                // 4. Armamos la respuesta con la estructura exacta que espera tu JS/TS
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
                // Podés cambiar el Console por un ILogger si tenés configurado
                Console.WriteLine($"Error al calcular tendencia: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno al calcular las tendencias." });
            }
        }

        // --------------- Funciones Auxiliares -------------------

        // Valida que la participación obtenida coinicida con [userId|sitioId]
        private async Task<bool> ValidarParticipacion(int userId, int sitioId, int participacionId){
            return await _context.Participaciones
            .AnyAsync(p => p.UsuarioSitioId == userId && p.SitioId == sitioId && p.Id == participacionId);
        }

        // Funcion auxiliar valida que la prediccion pertenezca al usuario id
        private async Task<bool> ValidarPrediccionExistente(int idUsuario, PrediccionDTO dto){
            return await _context.Predicciones
            .AnyAsync(p => p.Id == dto.Id && p.Participacion.UsuarioSitioId == idUsuario);
        }
        
        private async Task<bool> ValidarNuevaPrediccion(PrediccionDTO dto){
            return await _context.Participaciones
            .AnyAsync(p => p.Id == dto.ParticipacionId
                && p.PencaInstancia.Penca.Partidos.Any(partido => partido.Id == dto.PartidoId));
        }
        private bool ValidarFormatoResultado(PrediccionDTO dto){
            if(dto.GolesEquipoLocal < 0 || dto.GolesEquipoVisitante < 0){
                return false;
            }
            return true;
        }
        
        // Funcion auxiliar para comprobar estado del partido
        private async Task<bool> PartidoYaJugado(int partidoId){
            var partido = await _context.Partidos.FindAsync(partidoId);
            return partido?.Jugado == true;
        }
    }
}