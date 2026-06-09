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

    }


}
