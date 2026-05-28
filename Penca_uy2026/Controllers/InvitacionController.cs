using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.DTOs;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/invitacion")]
    [Authorize]
    public class InvitacionController : ControllerBase
    {
        private readonly InvitacionService _invitacionService;

        public InvitacionController(InvitacionService invitacionService)
        {
            _invitacionService = invitacionService;
        }

        /// <summary>
        /// Genera un nuevo token de invitación para un sitio determinado.
        /// </summary>
        [HttpPost("generar")]
        public async Task<IActionResult> GenerarInvitacion([FromBody] GenerarInvitacionRequest request)
        {
            var tokenSitioIdClaim = User.FindFirst("sitioId");
            if (tokenSitioIdClaim == null) return Unauthorized();

            int tokenSitioId = int.Parse(tokenSitioIdClaim.Value);

            var (response, errorMessage) = await _invitacionService.GenerarInvitacionAsync(tokenSitioId, request);

            if (response == null)
            {
                if (errorMessage == "FORBIDDEN") return Forbid();
                return BadRequest(new { mensaje = errorMessage });
            }

            return Ok(response);
        }

        /// <summary>
        /// Valida públicamente (sin estar logueado) si un código de invitación es válido para un sitio.
        /// </summary>
        [HttpGet("validar")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarInvitacion([FromQuery] string token, [FromQuery] string slug)
        {
            var isValid = await _invitacionService.ValidarInvitacionAsync(token, slug);
            return Ok(new { valido = isValid });
        }
    }
}
