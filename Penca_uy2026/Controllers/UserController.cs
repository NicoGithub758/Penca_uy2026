using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.DTOs;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UsuarioAuthService _usuarioAuthService;

        public UserController(UsuarioAuthService usuarioAuthService)
        {
            _usuarioAuthService = usuarioAuthService;
        }

        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDTO request)
        {
            // Se extrae el ID del usuario directamente del token JWT que envió el cliente.
            // El atributo [Authorize] ya se encargó de validar que el token sea legítimo.
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            var tokenSitioIdClaim = User.FindFirst("sitioId");

            if (userIdClaim == null || tokenSitioIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            int tokenSitioId = int.Parse(tokenSitioIdClaim.Value);

            // Se delega validaciones y el hasheo de la nueva pass al servicio.
            var (success, errorMessage) = await _usuarioAuthService.UpdatePasswordAsync(userId, tokenSitioId, request);

            if (!success)
            {
                if (errorMessage == "FORBIDDEN") return Forbid(); // 403, usuario tratando de joder.
                // Si el servicio devuelve false, retornamos un error 400 (Bad Request).
                return BadRequest(new { mensaje = errorMessage }); // 400
            }

            // Si todo salió bien, devolvemos un 200 (OK).
            return Ok(new { mensaje = "Contraseña actualizada correctamente." });
        }
    }
}
