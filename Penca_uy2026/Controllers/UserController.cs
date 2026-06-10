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
        private readonly ImageService _imageService;

        public UserController(UsuarioAuthService usuarioAuthService, ImageService imageService)
        {
            _usuarioAuthService = usuarioAuthService;
            _imageService = imageService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe([FromQuery] string slug)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            var tokenSitioIdClaim = User.FindFirst("sitioId");
            if (userIdClaim == null || tokenSitioIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            int tokenSitioId = int.Parse(tokenSitioIdClaim.Value);

            if (string.IsNullOrEmpty(slug)) return BadRequest(new { mensaje = "El slug es requerido." });

            var perfil = await _usuarioAuthService.GetUsuarioPerfilAsync(userId, tokenSitioId, slug);
            if (perfil == null) return Forbid();

            return Ok(perfil);
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

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            var tokenSitioIdClaim = User.FindFirst("sitioId");
            if (userIdClaim == null || tokenSitioIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            int tokenSitioId = int.Parse(tokenSitioIdClaim.Value);

            var (success, errorMessage) = await _usuarioAuthService.UpdateProfileAsync(userId, tokenSitioId, request);

            if (!success)
            {
                if (errorMessage == "FORBIDDEN") return Forbid();
                return BadRequest(new { mensaje = errorMessage ?? "No se pudo actualizar el perfil." });
            }

            return Ok(new { mensaje = "Perfil actualizado correctamente." });
        }

        /// <summary>
        /// Sube el archivo enviado desde el frontend a servicio de almacenamiento mediante el ImageService.
        /// Retorna la URL segura y actualiza el perfil del usuario.
        /// </summary>
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file, [FromForm] string slug)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            var tokenSitioIdClaim = User.FindFirst("sitioId");
            if (userIdClaim == null || tokenSitioIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            int tokenSitioId = int.Parse(tokenSitioIdClaim.Value);

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { mensaje = "No se proporcionó ningún archivo válido." });
            }

            // Validación de seguridad (Content-Type debe ser de imagen)
            if (!file.ContentType.StartsWith("image/"))
            {
                return BadRequest(new { mensaje = "El archivo debe ser una imagen." });
            }

            // Validar seguridad del sitio y del usuario ANTES de consumir recursos de Cloudinary
            bool tieneAcceso = await _usuarioAuthService.ValidarAccesoSitioYUsuarioAsync(userId, tokenSitioId, slug);
            if (!tieneAcceso)
            {
                return Forbid();
            }

            try
            {
                using var stream = file.OpenReadStream();
                
                // Se sube a Cloudinary usando el ImageService
                var secureUrl = await _imageService.UploadImageAsync(stream, file.FileName, $"tupenca/sitio_{tokenSitioId}");

                if (string.IsNullOrEmpty(secureUrl))
                {
                    return BadRequest(new { mensaje = "Error al subir la imagen a la nube." });
                }

                // Se actualiza el perfil de usuario en base de datos
                var updateDto = new UpdateProfileDTO { AvatarUrl = secureUrl, Slug = slug };
                var (success, errorMessage) = await _usuarioAuthService.UpdateProfileAsync(userId, tokenSitioId, updateDto);

                if (!success)
                {
                    if (errorMessage == "FORBIDDEN") return Forbid();
                    return BadRequest(new { mensaje = errorMessage ?? "Error al guardar la URL en el perfil." });
                }

                // Si todo sale bien, retornamos la nueva URL para que el Frontend la muestre de inmediato
                return Ok(new { url = secureUrl });
            }
            catch (Exception ex)
            {
                // En producción se usaría un ILogger para grabar el ex.Message, acá retornamos un error genérico 500
                return StatusCode(500, new { mensaje = "Error interno al procesar la imagen." });
            }
        }
    }
}
