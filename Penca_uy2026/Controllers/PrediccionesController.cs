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

        // Listado de partidos de una penca 
        [HttpGet("partidos")]
        public async Task<IActionResult> Partidos([FromQuery] int idParticipacion,[FromQuery] int idPenca)
        {
            var partidos = await _context.Partidos
                .Where(pe => pe.PencaId == idPenca).OrderBy(partido => partido.Jugado)
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
                                p.GolesEquipoVisitante
                            })
                            .FirstOrDefault()}).ToListAsync();

            return Ok(partidos);
        }

        // Valida y persiste predicción realizada por el usuario en una penca que participa
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> RealizarPrediccion([FromBody] PrediccionDTO prediccionDTO){
            
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if(await PartidoYaJugado(prediccionDTO.PartidoId))
            return BadRequest("No se puede predecir un partido ya jugado.");
            
            if(!ValidarFormatoResultado(prediccionDTO)){
                return BadRequest("Error en el formato de resultados.");
            }

            var prediccionExistente = await _context.Predicciones.FirstOrDefaultAsync(p => p.Id == prediccionDTO.Id);
            
            if(prediccionExistente != null){        // Existe prediccion
                if(await ValidarPrediccionExistente(usuarioId, prediccionDTO)){
                    prediccionExistente.GolesEquipoLocal = prediccionDTO.GolesEquipoLocal;
                    prediccionExistente.GolesEquipoVisitante = prediccionDTO.GolesEquipoVisitante;
                }
                else{
                    return BadRequest("Error: datos invalidos.");        
                }
            }else{          // Nueva prediccion
                if(await ValidarNuevaPenca(usuarioId, prediccionDTO)){
                    var nuevaPrediccion = new Prediccion
                    {
                        GolesEquipoLocal = prediccionDTO.GolesEquipoLocal,
                        GolesEquipoVisitante = prediccionDTO.GolesEquipoVisitante,
                        PuntosObtenidos = 0,
                        ParticipacionId = prediccionDTO.ParticipacionId,
                        PartidoId = prediccionDTO.PartidoId,
                        SitioId = prediccionDTO.SitioId,
                    };
                    _context.Predicciones.Add(nuevaPrediccion);
                }else{
                    return BadRequest("Error: datos invalidos.");        
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
            
        }
        

        // --------------- Funciones Auxiliares -------------------

        // Funcion auxiliar valida que la prediccion pertenezca al usuario id
        private async Task<bool> ValidarPrediccionExistente(int idUsuario, PrediccionDTO dto){
            return await _context.Predicciones
            .AnyAsync(p => p.Id == dto.Id && p.Participacion.UsuarioSitioId == idUsuario);
        }
        
        private async Task<bool> ValidarNuevaPenca(int idUsuario, PrediccionDTO dto){
            return await _context.Participaciones
            .AnyAsync(p => p.SitioId == dto.SitioId && p.UsuarioSitioId == idUsuario
            && p.PencaInstancia.PencaId == dto.Id && p.PencaInstancia.Penca.Partidos.Any(partido => partido.Id == dto.PartidoId));
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