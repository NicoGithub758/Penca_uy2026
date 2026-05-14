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
        public async Task<SocialLoginResponse?> LoginTradicionalAsync(string email, string password, string? slug = null)
        {
            // Se busca al usuario en la base de datos filtrando por email y, opcionalmente, por el slug del sitio.
            var query = _context.UsuariosSitio.Include(u => u.Sitio).AsQueryable();

            if (!string.IsNullOrEmpty(slug))
            {
                query = query.Where(u => u.Sitio.Slug == slug);
            }

            var usuario = await query.FirstOrDefaultAsync(u => u.Email == email);

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
        /// Registra un nuevo usuario en un sitio específico, hasheando su contraseña con BCrypt.
        /// </summary>
        /// <param name="request">Datos del registro.</param>
        /// <returns>Datos del usuario y JWT si el registro es exitoso; null si el usuario ya existe.</returns>
        public async Task<SocialLoginResponse?> RegistrarUsuarioAsync(RegisterRequest request)
        {
            int sitioId = request.SitioId;

            // Si se proporciona un slug, se resuelve el SitioId correspondiente.
            if (!string.IsNullOrEmpty(request.Slug))
            {
                var sitio = await _context.Sitios.FirstOrDefaultAsync(s => s.Slug == request.Slug);
                if (sitio == null) return null;
                sitioId = sitio.Id;
            }

            if (sitioId <= 0) return null;

            // Se verifica si ya existe un usuario con el mismo email para el sitio dado.
            bool existe = await _context.UsuariosSitio
                .AnyAsync(u => u.Email == request.Email && u.SitioId == sitioId);

            if (existe)
            {
                return null;
            }

            // Se genera el hash de la contraseña de forma segura.
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var nuevoUsuario = new UsuarioSitio
            {
                Nombre = request.Nombre,
                Email = request.Email,
                PasswordHash = passwordHash,
                SitioId = sitioId,
                Activo = true,
                FechaRegistro = DateTime.UtcNow,
                Rol = RolUsuarioSitio.Jugador
            };

            _context.UsuariosSitio.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            // Se genera el JWT para que el usuario quede logueado inmediatamente tras el registro.
            var jwt = _tokenService.GenerarJwtParaUsuario(nuevoUsuario);

            return new SocialLoginResponse
            {
                Jwt = jwt,
                UsuarioSitioId = nuevoUsuario.Id,
                SitioId = nuevoUsuario.SitioId,
                Nombre = nuevoUsuario.Nombre,
                Email = nuevoUsuario.Email
            };
        }

        /// <summary>
        /// Procesa la autenticación social mediante Auth0/Google.
        /// Valida el token externo y asegura la existencia del perfil local del usuario.
        /// </summary>
        public async Task<SocialLoginResponse?> LoginGoogleAsync(string auth0Token, int sitioId, string? slug = null)
        {
            // 1. Se valida el token externo contra el endpoint de información de usuario de Auth0.
            var auth0User = await ValidarTokenAuth0Async(auth0Token);
            if (auth0User == null)
            {
                Console.WriteLine("DEBUG: Falló la validación del token contra Auth0 /userinfo.");
                return null;
            }

            // 2. Se confirma que el sitio destino esté disponible para operación.
            Sitio? sitio = null;
            if (!string.IsNullOrEmpty(slug))
            {
                sitio = await _context.Sitios.FirstOrDefaultAsync(s => s.Slug == slug);
            }
            else
            {
                sitio = await _context.Sitios.FindAsync(sitioId);
            }

            if (sitio == null)
            {
                Console.WriteLine($"DEBUG: No se encontró el sitio (ID: {sitioId}, Slug: {slug}) en la base de datos.");
                return null;
            }
            
            if (!sitio.Activo)
            {
                Console.WriteLine($"DEBUG: El sitio {sitio.Id} existe pero no está activo.");
                return null;
            }

            sitioId = sitio.Id;

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
