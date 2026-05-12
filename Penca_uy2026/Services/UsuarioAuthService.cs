using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;
using System.Net.Http.Json;

namespace Penca_uy2026.Services
{
    /// <summary>
    /// Servicio encargado de la lógica de negocio relacionada con la autenticación de usuarios web.
    /// Maneja validaciones de credenciales internas y login social.
    /// </summary>
    public class UsuarioAuthService
    {
        private readonly MyDbContext _context;
        private readonly TokenService _tokenService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public UsuarioAuthService(
            MyDbContext context, 
            TokenService tokenService, 
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _httpClient = httpClientFactory.CreateClient();
            _config = config;
        }

        /// <summary>
        /// Procesa el inicio de sesión con correo y contraseña.
        /// </summary>
        /// <returns>Objeto con el JWT y los datos del usuario si las credenciales son válidas; de lo contrario, null.</returns>
        public async Task<SocialLoginResponse?> LoginTradicionalAsync(string email, string password)
        {
            // Se busca al usuario en la base de datos incluyendo su relación con el Sitio.
            var usuario = await _context.UsuariosSitio
                .Include(u => u.Sitio)
                .FirstOrDefaultAsync(u => u.Email == email);

            // Se verifica la existencia del usuario y la validez de la contraseña mediante el hash de BCrypt.
            if (usuario == null || string.IsNullOrEmpty(usuario.PasswordHash) || 
                !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash) || !usuario.Activo)
            {
                return null;
            }

            // Se delega la creación del token al servicio especializado.
            var jwt = _tokenService.GenerarJwtParaUsuario(usuario);

            return new SocialLoginResponse
            {
                Jwt = jwt,
                UsuarioSitioId = usuario.Id,
                SitioId = usuario.SitioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email
            };
        }

        /// <summary>
        /// Procesa la autenticación social mediante Auth0/Google.
        /// Valida el token externo y asegura la existencia del perfil local del usuario.
        /// </summary>
        public async Task<SocialLoginResponse?> LoginGoogleAsync(string auth0Token, int sitioId)
        {
            // 1. Se valida el token externo contra el endpoint de información de usuario de Auth0.
            var auth0User = await ValidarTokenAuth0Async(auth0Token);
            if (auth0User == null)
            {
                Console.WriteLine("DEBUG: Falló la validación del token contra Auth0 /userinfo.");
                return null;
            }

            // 2. Se confirma que el sitio destino esté disponible para operación.
            var sitio = await _context.Sitios.FindAsync(sitioId);
            if (sitio == null)
            {
                Console.WriteLine($"DEBUG: No se encontró el sitio con ID {sitioId} en la base de datos.");
                return null;
            }
            
            if (!sitio.Activo)
            {
                Console.WriteLine($"DEBUG: El sitio {sitioId} existe pero no está activo.");
                return null;
            }

            // 3. Se busca al usuario local o se procede a su creación (Sincronización).
            var usuario = await _context.UsuariosSitio
                .FirstOrDefaultAsync(u => u.Auth0Id == auth0User.Sub && u.SitioId == sitioId);

            if (usuario == null)
            {
                usuario = new UsuarioSitio
                {
                    Auth0Id = auth0User.Sub,
                    Nombre = auth0User.Name ?? auth0User.Email,
                    Email = auth0User.Email,
                    SitioId = sitioId,
                    Activo = true,
                    FechaRegistro = DateTime.UtcNow,
                    Rol = RolUsuarioSitio.Jugador
                };
                _context.UsuariosSitio.Add(usuario);
            }
            else
            {
                // Se actualiza la información que podría haber cambiado en el proveedor externo.
                usuario.Nombre = auth0User.Name ?? usuario.Nombre;
            }

            await _context.SaveChangesAsync();

            // 4. Se genera el token de identidad propio de la plataforma.
            var jwt = _tokenService.GenerarJwtParaUsuario(usuario);

            return new SocialLoginResponse
            {
                Jwt = jwt,
                UsuarioSitioId = usuario.Id,
                SitioId = usuario.SitioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email
            };
        }

        /// <summary>
        /// Consulta al proveedor Auth0 para validar un Bearer token.
        /// </summary>
        private async Task<Auth0UserInfo?> ValidarTokenAuth0Async(string token)
        {
            try
            {
                var domain = _config["Auth0:Domain"];
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://{domain}/userinfo");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                return await response.Content.ReadFromJsonAsync<Auth0UserInfo>();
            }
            catch
            {
                return null;
            }
        }
    }
}
