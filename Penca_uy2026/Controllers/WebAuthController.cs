using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.DTOs;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    /// <summary>
    /// Controlador encargado de gestionar los procesos de autenticación para los usuarios web.
    /// Actúa como punto de entrada (Gateway) delegando la lógica de negocio al servicio UsuarioAuthService.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class WebAuthController : ControllerBase
    {
        private readonly UsuarioAuthService _usuarioAuthService;

        public WebAuthController(UsuarioAuthService usuarioAuthService)
        {
            _usuarioAuthService = usuarioAuthService;
        }

        /// <summary>
        /// Endpoint para el inicio de sesión tradicional mediante credenciales internas.
        /// POST /api/auth/login
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Se delega la validación y generación del resultado al servicio de autenticación.
            var result = await _usuarioAuthService.LoginTradicionalAsync(request.Email, request.Password, request.Slug);

            if (result == null)
            {
                return Unauthorized(new { mensaje = "El correo electrónico o la contraseña son incorrectos, o el usuario está inactivo en este sitio." });
            }

            // Se retorna el token JWT y el perfil del usuario al cliente React.
            return Ok(new
            {
                jwt = result.Jwt,
                usuario = new
                {
                    id = result.UsuarioSitioId,
                    nombre = result.Nombre,
                    email = result.Email,
                    sitioId = result.SitioId,
                    rol = result.Rol,
                    tienePassword = result.TienePassword
                }
            });
        }

        /// <summary>
        /// Endpoint para el registro de nuevos usuarios.
        /// POST /api/auth/register
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _usuarioAuthService.RegistrarUsuarioAsync(request);

            if (result == null)
            {
                return BadRequest(new { mensaje = "El usuario ya está registrado en este sitio o los datos son inválidos." });
            }

            // Si el JWT viene vacío, significa que el registro quedó pendiente de aprobación (Tipo 1 o 2).
            if (string.IsNullOrEmpty(result.Jwt))
            {
                return Accepted(new { status = "pending", mensaje = "Tu solicitud ha sido enviada y está a la espera de aprobación por un administrador." });
            }

            return Ok(new
            {
                jwt = result.Jwt,
                usuario = new
                {
                    id = result.UsuarioSitioId,
                    nombre = result.Nombre,
                    email = result.Email,
                    sitioId = result.SitioId,
                    rol = result.Rol,
                    tienePassword = result.TienePassword
                }
            });
        }

        /// <summary>
        /// Endpoint para el inicio de sesión social mediante Google (vía Auth0).
        /// POST /api/auth/google
        /// </summary>
        [HttpPost("google")]
        public async Task<IActionResult> LoginGoogle([FromBody] WebSocialLoginRequest request)
        {
            Console.WriteLine("DEBUG: Petición recibida en WebAuthController -> LoginGoogle");
            // Se delega el flujo de autenticación social al servicio correspondiente.
            var (data, errorMessage) = await _usuarioAuthService.LoginGoogleAsync(request.Auth0Token, request.SitioId, request.Slug);

            if (data == null)
            {
                return Unauthorized(new { mensaje = errorMessage ?? "No se pudo validar la identidad con Google o el sitio seleccionado no es válido." });
            }

            return Ok(new
            {
                jwt = data.Jwt,
                usuario = new
                {
                    id = data.UsuarioSitioId,
                    nombre = data.Nombre,
                    email = data.Email,
                    sitioId = data.SitioId,
                    rol = data.Rol,
                    tienePassword = data.TienePassword
                }
            });
        }
    }
}
