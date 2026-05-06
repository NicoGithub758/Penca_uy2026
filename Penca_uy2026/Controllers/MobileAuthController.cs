using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;
using Penca_uy2026.Services;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/mobile")]
    public class MobileAuthController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly MobileAuthService _mobileAuthService;

        public MobileAuthController(MyDbContext context, MobileAuthService mobileAuthService)
        {
            _context = context;
            _mobileAuthService = mobileAuthService;
        }

        /// <summary>
        /// Devuelve la lista de sitios activos para mostrar en la app móvil.
        /// GET /api/mobile/sitios
        /// </summary>
        [HttpGet("sitios")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSitios()
        {
            var sitios = await _context.Sitios
                .Where(s => s.Activo)
                .Select(s => new SitioDto
                {
                    Id = s.Id,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    LogoUrl = s.LogoUrl,
                    ColorPrincipal = s.ColorPrincipal,
                    TipoRegistro = s.TipoRegistro.ToString()
                })
                .ToListAsync();

            return Ok(sitios);
        }

        /// <summary>
        /// Login social con Google via Auth0.
        /// Crea el usuario en el sitio si no existe.
        /// POST /api/mobile/auth/social
        /// </summary>
        [HttpPost("auth/social")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginSocial([FromBody] SocialLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Auth0Token) || request.SitioId <= 0)
                return BadRequest("Token y SitioId son requeridos.");

            var result = await _mobileAuthService.LoginSocialAsync(request);

            if (result == null)
                return Unauthorized("Token inválido o sitio no encontrado.");

            return Ok(result);
        }

        /// <summary>
        /// Registra o actualiza el token FCM del dispositivo del usuario.
        /// POST /api/mobile/auth/fcm-token
        /// </summary>
        [HttpPost("auth/fcm-token")]
        [Authorize(Roles = "UsuarioSitio")]
        public async Task<IActionResult> GuardarFcmToken([FromBody] FcmTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.FcmToken))
                return BadRequest("El token FCM es requerido.");

            // Obtener el ID del usuario desde el JWT
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdClaim, out int usuarioSitioId))
                return Unauthorized();

            var ok = await _mobileAuthService.GuardarFcmTokenAsync(usuarioSitioId, request.FcmToken);

            if (!ok) return NotFound("Usuario no encontrado.");

            return Ok(new { mensaje = "Token FCM registrado correctamente." });
        }

        /// <summary>
        /// Devuelve el perfil del usuario autenticado.
        /// GET /api/mobile/perfil
        /// </summary>
        [HttpGet("perfil")]
        [Authorize(Roles = "UsuarioSitio")]
        public async Task<IActionResult> GetPerfil()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdClaim, out int usuarioSitioId))
                return Unauthorized();

            var usuario = await _context.UsuariosSitio
                .Include(u => u.Sitio)
                .FirstOrDefaultAsync(u => u.Id == usuarioSitioId);

            if (usuario == null) return NotFound();

            return Ok(new
            {
                usuario.Id,
                usuario.Nombre,
                usuario.Email,
                usuario.SitioId,
                SitioNombre = usuario.Sitio.Nombre,
                usuario.FechaRegistro
            });
        }
    }
}
