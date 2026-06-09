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
        public async Task<WebSocialLoginResponse?> LoginTradicionalAsync(string email, string password, string? slug = null)
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

            return new WebSocialLoginResponse
            {
                Jwt = jwt,
                UsuarioSitioId = usuario.Id,
                SitioId = usuario.SitioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                TienePassword = !string.IsNullOrEmpty(usuario.PasswordHash),
                Rol = (int)usuario.Rol
            };
        }

        /// <summary>
        /// Registra un nuevo usuario en un sitio específico, hasheando su contraseña con BCrypt.
        /// </summary>
        /// <param name="request">Datos del registro.</param>
        /// <returns>Datos del usuario y JWT si el registro es exitoso; null si el usuario ya existe.</returns>
        public async Task<WebSocialLoginResponse?> RegistrarUsuarioAsync(RegisterRequest request)
        {
            var sitio = (string.IsNullOrEmpty(request.Slug))
                ? await _context.Sitios.FirstOrDefaultAsync(s => (s.Id == request.SitioId))
                : await _context.Sitios.FirstOrDefaultAsync(s => s.Slug == request.Slug);
            if (sitio == null) return null;

            int sitioId = sitio.Id;

            // Se verifica si ya existe un usuario con el mismo email para el sitio dado.
            bool existe = await _context.UsuariosSitio
                .AnyAsync(u => u.Email == request.Email && u.SitioId == sitioId);
            if (existe) return null;

            if (sitio.TipoRegistro == TipoRegistro.Cerrada) return null;

            // Contraseña va a tener venga por el camino que venga (por lo menos por ahora)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 1. FLUJO CON SOLICITUD
            if (sitio.TipoRegistro == TipoRegistro.AbiertaConAutorizacion || sitio.TipoRegistro == TipoRegistro.SoloConInvitacion)
            {
                if(sitio.TipoRegistro == TipoRegistro.SoloConInvitacion)
                {
                    if (string.IsNullOrEmpty(request.TokenInvitacion)) return null;

                    var invitacion = await _context.Invitaciones.FirstOrDefaultAsync(i => (i.Token == request.TokenInvitacion && i.SitioId == sitioId && i.UsosDisponibles > 0));
                    if (invitacion == null) return null;

                    // En este punto sabemos que el intento de registro es válido y su invitación existe y tiene usos disponibles, por ende, procedemos a descontar un uso y borrar si quedó sin usos, ya que es un hecho que se utilizó.
                    invitacion.UsosDisponibles--;
                    if (invitacion.UsosDisponibles <= 0) _context.Invitaciones.Remove(invitacion);
                }

                // Lógica común para ambos, la solicitud se crea igual sea un camino u otro.

                var nuevaSolicitud = new SolicitudIngreso
                {
                    Nombre = request.Nombre,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    SitioId = sitioId,
                    FuePorInvitacion = (sitio.TipoRegistro == TipoRegistro.SoloConInvitacion),
                    Estado = EstadoSolicitud.Pendiente
                };

                _context.SolicitudesIngreso.Add(nuevaSolicitud);
                await _context.SaveChangesAsync();

                // Devolvemos JWT vacío asoprópitamente para que no se loguee al usuario.
                return new WebSocialLoginResponse
                {
                    Jwt = string.Empty,
                    UsuarioSitioId = 0, // Id inexistente simplemente, porque aún no hay.
                    SitioId = sitioId,
                    Nombre = request.Nombre,
                    Email = request.Email,
                    TienePassword = true
                };
            }

            // 2. Bloque de REGISTRO ABIERTO (Directo)
            
            var nuevoUsuario = new UsuarioSitio
            {
                Nombre = request.Nombre,
                Email = request.Email,
                PasswordHash = passwordHash,
                SitioId = sitioId,
                Origen = OrigenRegistro.Abierto, 
                Activo = true,
                FechaRegistro = DateTime.UtcNow,
                Rol = RolUsuarioSitio.Jugador
            };

            _context.UsuariosSitio.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            // Se genera el JWT para que el usuario quede logueado inmediatamente tras el registro.
            var jwt = _tokenService.GenerarJwtParaUsuario(nuevoUsuario);

            return new WebSocialLoginResponse
            {
                Jwt = jwt,
                UsuarioSitioId = nuevoUsuario.Id,
                SitioId = nuevoUsuario.SitioId,
                Nombre = nuevoUsuario.Nombre,
                Email = nuevoUsuario.Email,
                TienePassword = true, // Se acaba de registrar con password
                Rol = (int)nuevoUsuario.Rol
            };
        }

        /// <summary>
        /// Procesa la autenticación social mediante Auth0/Google.
        /// Valida el token externo y asegura la existencia del perfil local del usuario.
        /// Retorna una tupla con los datos si fue exitoso, o un mensaje de error detallado.
        /// </summary>
        public async Task<(WebSocialLoginResponse? Data, string? ErrorMessage)> LoginGoogleAsync(string auth0Token, int sitioId, string? slug = null)
        {
            // 1. Se valida el token externo contra el endpoint de información de usuario de Auth0.
            var auth0User = await ValidarTokenAuth0Async(auth0Token);
            if (auth0User == null)
            {
                Console.WriteLine("DEBUG: Falló la validación del token contra Auth0 /userinfo.");
                return (null, "El token de Google proporcionado no es válido.");
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

            if (sitio == null || !sitio.Activo)
            {
                return (null, "El sitio seleccionado no existe o no está activo.");
            }

            sitioId = sitio.Id;

            // 3. Se busca al usuario local.
            var usuario = await _context.UsuariosSitio
                .FirstOrDefaultAsync(u => u.Auth0Id == auth0User.Sub && u.SitioId == sitioId);

            if (usuario == null)
            {
                // Verificamos si existe alguien registrado de forma tradicional con el mismo email
                bool emailEnUso = await _context.UsuariosSitio.AnyAsync(u => u.Email == auth0User.Email && u.SitioId == sitioId);
                
                if (emailEnUso)
                {
                    // Como buena práctica de seguridad y UX, no auto-vinculamos cuentas sin consentimiento explícito.
                    return (null, "Ya existe una cuenta tradicional con este correo. Inicia sesión con tus credenciales y asocia Google desde tu perfil.");
                }

                // El usuario es totalmente nuevo para este sitio. Verificamos las reglas del sitio.
                if (sitio.TipoRegistro != TipoRegistro.Abierta)
                {
                    return (null, "Este sitio no admite registros directos mediante Google. Por favor, utiliza el método de registro que corresponda.");
                }

                // Es abierto, lo creamos directamente.
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

            return (new WebSocialLoginResponse
            {
                Jwt = jwt,
                UsuarioSitioId = usuario.Id,
                SitioId = usuario.SitioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                TienePassword = !string.IsNullOrEmpty(usuario.PasswordHash),
                Rol = (int)usuario.Rol
            }, null);
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

        public async Task<(bool Success, string? ErrorMessage)> UpdatePasswordAsync(int userId, int tokenSitioId, UpdatePasswordDTO request)
        {
            var sitio = await _context.Sitios.FirstOrDefaultAsync(s => s.Slug == request.Slug);
            if (sitio == null) return (false, "El sitio especificado no existe.");

            // Seguridad
            if(tokenSitioId != sitio.Id)
            {
                return (false, "FORBIDDEN"); // Para que el controller devuelva 403, el usuario se intento hacer el inteligente.
            }

            var usuario = await _context.UsuariosSitio.FindAsync(userId);
            if(usuario == null) return (false, "Usuario no encontrado.");

            // Si el usuario ya tiene contraseña, debemos validar que oldPassword sea correcta antes de actualizar su contraseña.
            if(!string.IsNullOrEmpty(usuario.PasswordHash))
            {
                if(string.IsNullOrEmpty(request.OldPassword) || !BCrypt.Net.BCrypt.Verify(request.OldPassword, usuario.PasswordHash))
                {
                    // La contraseña anterior venía vacía o venía correctamente indicada pero no es la que ya tiene el usuario.
                    return (false, "La contraseña actual es incorrecta.");
                }
            }

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
