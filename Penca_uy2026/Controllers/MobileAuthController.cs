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
        private readonly UsuarioAuthService _usuarioAuthService;

        public MobileAuthController(
            MyDbContext context,
            MobileAuthService mobileAuthService,
            UsuarioAuthService usuarioAuthService)
        {
            _context = context;
            _mobileAuthService = mobileAuthService;
            _usuarioAuthService = usuarioAuthService;
        }

        // ---------------------------------------------------------------------
        //  ENDPOINTS PUBLICOS (sin autenticacion)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Devuelve la lista de sitios activos para mostrar en la app movil.
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
        /// Login interno (email + password) desde mobile.
        /// Reutiliza UsuarioAuthService que ya valida BCrypt y estado del usuario.
        /// POST /api/mobile/auth/login
        /// </summary>
        [HttpPost("auth/login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginInterno([FromBody] MobileLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || request.SitioId <= 0)
                return BadRequest(new { mensaje = "Email, password y sitioId son requeridos." });

            // Reusamos LoginTradicionalAsync. No le pasamos slug (mobile no tiene).
            var result = await _usuarioAuthService.LoginTradicionalAsync(
                request.Email, request.Password, slug: null);

            if (result == null)
                return Unauthorized(new { mensaje = "Credenciales invalidas o usuario inactivo." });

            // Validamos que el usuario pertenezca al sitio que el mobile esta usando.
            // Esto evita que alguien con cuenta en otro sitio se loguee desde la app de este sitio.
            if (result.SitioId != request.SitioId)
                return Unauthorized(new { mensaje = "El usuario no pertenece a este sitio." });

            return Ok(new MobileAuthResponse
            {
                Jwt = result.Jwt,
                UsuarioSitioId = result.UsuarioSitioId,
                SitioId = result.SitioId,
                Nombre = result.Nombre,
                Email = result.Email,
                EstadoSolicitud = "Activo"
            });
        }

        /// <summary>
        /// Registro de usuario desde mobile.
        /// Reutiliza UsuarioAuthService.RegistrarUsuarioAsync que maneja todos los TipoRegistro.
        /// POST /api/mobile/auth/register
        /// </summary>
        [HttpPost("auth/register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] MobileRegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Nombre) || string.IsNullOrEmpty(request.Email)
                || string.IsNullOrEmpty(request.Password) || request.SitioId <= 0)
                return BadRequest(new { mensaje = "Todos los campos son requeridos." });

            // Mapeamos al DTO que espera el servicio compartido.
            var serviceRequest = new RegisterRequest
            {
                Nombre = request.Nombre,
                Email = request.Email,
                Password = request.Password,
                SitioId = request.SitioId,
                Slug = null,  // Mobile no usa slug
                TokenInvitacion = request.TokenInvitacion
            };

            var result = await _usuarioAuthService.RegistrarUsuarioAsync(serviceRequest);

            if (result == null)
                return BadRequest(new { mensaje = "El usuario ya esta registrado, el sitio esta cerrado, o los datos son invalidos." });

            // Si el JWT viene vacio, el registro quedo pendiente (sitios con autorizacion o invitacion).
            if (string.IsNullOrEmpty(result.Jwt))
            {
                return Accepted(new MobileAuthResponse
                {
                    Jwt = string.Empty,
                    UsuarioSitioId = 0,
                    SitioId = result.SitioId,
                    Nombre = result.Nombre,
                    Email = result.Email,
                    EstadoSolicitud = "Pendiente",
                    Mensaje = "Tu solicitud ha sido enviada y esta a la espera de aprobacion."
                });
            }

            // Registro abierto exitoso: JWT generado.
            return Ok(new MobileAuthResponse
            {
                Jwt = result.Jwt,
                UsuarioSitioId = result.UsuarioSitioId,
                SitioId = result.SitioId,
                Nombre = result.Nombre,
                Email = result.Email,
                EstadoSolicitud = "Activo"
            });
        }

        /// <summary>
        /// Login social con Google via Auth0.
        /// Reemplaza la version anterior: ahora valida TipoRegistro (delega en UsuarioAuthService).
        /// Google solo registra usuarios nuevos en sitios con TipoRegistro = Abierta.
        /// POST /api/mobile/auth/social
        /// </summary>
        [HttpPost("auth/social")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginSocial([FromBody] SocialLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Auth0Token) || request.SitioId <= 0)
                return BadRequest(new { mensaje = "Token y SitioId son requeridos." });

            // Reusamos LoginGoogleAsync del servicio web (este si respeta TipoRegistro).
            var (data, errorMessage) = await _usuarioAuthService.LoginGoogleAsync(
                request.Auth0Token, request.SitioId, slug: null);

            if (data == null)
                return Unauthorized(new { mensaje = errorMessage ?? "No se pudo validar la identidad con Google." });

            return Ok(new MobileAuthResponse
            {
                Jwt = data.Jwt,
                UsuarioSitioId = data.UsuarioSitioId,
                SitioId = data.SitioId,
                Nombre = data.Nombre,
                Email = data.Email,
                EstadoSolicitud = "Activo"
            });
        }

        // ---------------------------------------------------------------------
        //  ENDPOINTS AUTENTICADOS
        // ---------------------------------------------------------------------

        /// <summary>
        /// Registra o actualiza el token FCM del dispositivo del usuario.
        /// POST /api/mobile/auth/fcm-token
        /// </summary>
        [HttpPost("auth/fcm-token")]
        [Authorize(Roles = "Jugador")]
        public async Task<IActionResult> GuardarFcmToken([FromBody] FcmTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.FcmToken))
                return BadRequest(new { mensaje = "El token FCM es requerido." });

            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdClaim, out int usuarioSitioId))
                return Unauthorized();

            var ok = await _mobileAuthService.GuardarFcmTokenAsync(usuarioSitioId, request.FcmToken);

            if (!ok) return NotFound(new { mensaje = "Usuario no encontrado." });

            return Ok(new { mensaje = "Token FCM registrado correctamente." });
        }

        /// <summary>
        /// Limpia el FCM token al cerrar sesion para evitar notificaciones cruzadas
        /// si otro usuario inicia sesion en el mismo dispositivo.
        /// DELETE /api/mobile/auth/fcm-token
        /// </summary>
        [HttpDelete("auth/fcm-token")]
        [Authorize(Roles = "Jugador")]
        public async Task<IActionResult> LimpiarFcmToken()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdClaim, out int usuarioSitioId))
                return Unauthorized();

            var usuario = await _context.UsuariosSitio.FirstOrDefaultAsync(u => u.Id == usuarioSitioId);
            if (usuario == null) return NotFound();

            usuario.FcmToken = null;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Token FCM eliminado." });
        }

        /// <summary>
        /// Devuelve el perfil del usuario autenticado.
        /// GET /api/mobile/perfil
        /// </summary>
        [HttpGet("perfil")]
        [Authorize(Roles = "Jugador")]
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
