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
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            // Se delega la validación de la contraseña actual y el hasheo de la nueva al servicio.
            var success = await _usuarioAuthService.UpdatePasswordAsync(userId, request);

            if (!success)
            {
                // Si el servicio devuelve false, retornamos un error 400 (Bad Request).
                return BadRequest(new { mensaje = "No se pudo actualizar la contraseña. Verifique que la contraseña actual sea correcta." });
            }

            // Si todo salió bien, devolvemos un 200 (OK).
            return Ok(new { mensaje = "Contraseña actualizada correctamente." });
        }
    }
}
