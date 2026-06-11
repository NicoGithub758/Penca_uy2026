using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [Route("api/pencas")]
    [ApiController]
    public class PencasApiController : ControllerBase
    {
        private readonly MyDbContext _context;

        public PencasApiController(MyDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene la lista de pencas disponibles para un sitio específico, 
        /// indicando en cuáles de ellas ya está participando el usuario autenticado.
        /// Sólo accesible para usuarios autenticados en dicho sitio.
        /// </summary>
        [HttpGet("{slug}")]
        [Authorize]
        public async Task<IActionResult> GetPencasDelSitio(string slug)
        {
            // 1. Obtener el sitio por su slug
            var sitio = await _context.Sitios.FirstOrDefaultAsync(s => s.Slug == slug);
            if (sitio == null) return NotFound("Sitio no encontrado.");

            // 2. Seguridad: Validar que el usuario pertenece a este sitio (CBAC)
            var sitioIdClaim = User.FindFirst("sitioId")?.Value;
            if (string.IsNullOrEmpty(sitioIdClaim) || !int.TryParse(sitioIdClaim, out int tokenSitioId))
            {
                return Unauthorized("Token inválido o sin sitio asociado.");
            }

            if (tokenSitioId != sitio.Id)
            {
                // El usuario está logueado pero en otro sitio distinto al solicitado en el slug
                return StatusCode(403, "No tienes permiso para acceder a las pencas de este sitio.");
            }

            // 3. Obtener el ID del usuario logueado
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out int usuarioSitioId))
            {
                return Unauthorized("Token inválido.");
            }

            // 4. Consultar las instancias de penca asociadas al sitio
            var pencas = await _context.PencaInstancias
                .Include(pi => pi.Penca)
                .Where(pi => pi.SitioId == sitio.Id)
                .Select(pi => new
                {
                    Id = pi.Id, // ID de la instancia, necesario para pagar
                    PencaId = pi.PencaId,
                    Nombre = pi.Penca.Nombre,
                    Deporte = pi.Penca.Deporte != null ? pi.Penca.Deporte.Nombre : "Desconocido",
                    Costo = pi.Costo,
                    // Verificamos si el usuario actual ya tiene una participación
                    YaParticipa = _context.Participaciones.Any(p => p.PencaInstanciaId == pi.Id && p.UsuarioSitioId == usuarioSitioId),
                    IdParticipacion = _context.Participaciones
                        .Where(p => p.PencaInstanciaId == pi.Id && p.UsuarioSitioId == usuarioSitioId)
                        .Select(p => p.Id)
                        .FirstOrDefault()
 
                })
                .ToListAsync();

            return Ok(pencas);
        }

        /// <summary>
        /// Obtiene los partidos de una instancia de penca específica, 
        /// incluyendo las predicciones del usuario actual.
        /// </summary>
        [HttpGet("{slug}/{pencaInstanciaId}/partidos")]
        [Authorize]
        public async Task<IActionResult> GetPartidosPenca(string slug, int pencaInstanciaId)
        {
            // 1. Obtener el sitio por su slug
            var sitio = await _context.Sitios.FirstOrDefaultAsync(s => s.Slug == slug);
            if (sitio == null) return NotFound("Sitio no encontrado.");

            // 2. Seguridad: Validar que el usuario pertenece a este sitio (CBAC)
            var sitioIdClaim = User.FindFirst("sitioId")?.Value;
            if (string.IsNullOrEmpty(sitioIdClaim) || !int.TryParse(sitioIdClaim, out int tokenSitioId) || tokenSitioId != sitio.Id)
            {
                return StatusCode(403, "No tienes permiso para acceder a las pencas de este sitio.");
            }

            // 3. Obtener el ID del usuario logueado
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(usuarioIdClaim) || !int.TryParse(usuarioIdClaim, out int usuarioSitioId))
            {
                return Unauthorized("Token inválido.");
            }

            // 4. Obtener la participación del usuario en esta penca
            var participacion = await _context.Participaciones
                .Include(p => p.PencaInstancia)
                .FirstOrDefaultAsync(p => p.PencaInstanciaId == pencaInstanciaId && p.UsuarioSitioId == usuarioSitioId);

            if (participacion == null)
            {
                return BadRequest("No participas en esta penca o la participación no es válida.");
            }

            // 5. Devolver los partidos con las predicciones del usuario integradas
            var partidos = await _context.Partidos
                .Where(p => p.PencaId == participacion.PencaInstancia.PencaId)
                .OrderBy(p => p.Jugado).ThenBy(p => p.Fecha)
                .Select(partido => new
                {
                    partido.Id,
                    Local = new { 
                        partido.Local.Id, 
                        partido.Local.Nombre, 
                        partido.Local.LogoUrl 
                    },
                    Visitante = new { 
                        partido.Visitante.Id, 
                        partido.Visitante.Nombre, 
                        partido.Visitante.LogoUrl
                    },
                    partido.Fecha,
                    partido.GolesLocal,
                    partido.GolesVisitante,
                    partido.Jugado,
                    Prediccion = _context.Predicciones
                        .Where(p => p.ParticipacionId == participacion.Id && p.PartidoId == partido.Id)
                        .Select(p => new
                        {
                            p.Id,
                            p.GolesEquipoLocal,
                            p.GolesEquipoVisitante,
                            p.PuntosObtenidos
                        })
                        .FirstOrDefault()
                }).ToListAsync();

            return Ok(partidos);
        }

    }


}
