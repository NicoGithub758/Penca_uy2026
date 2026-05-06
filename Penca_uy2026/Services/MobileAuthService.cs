using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;

namespace Penca_uy2026.Services
{
    public class MobileAuthService
    {
        private readonly MyDbContext _context;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public MobileAuthService(MyDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Valida el token de Auth0, busca o crea el UsuarioSitio y devuelve un JWT propio.
        /// </summary>
        public async Task<SocialLoginResponse?> LoginSocialAsync(SocialLoginRequest request)
        {
            // 1. Validar el token con Auth0 y obtener info del usuario
            var auth0User = await ValidarTokenAuth0Async(request.Auth0Token);
            if (auth0User == null) return null;

            // 2. Verificar que el sitio existe y está activo
            var sitio = await _context.Sitios.FindAsync(request.SitioId);
            if (sitio == null || !sitio.Activo) return null;

            // 3. Buscar o crear el UsuarioSitio
            var usuario = await _context.UsuariosSitio
                .FirstOrDefaultAsync(u => u.Auth0Id == auth0User.Sub && u.SitioId == request.SitioId);

            if (usuario == null)
            {
                // Primera vez que este usuario accede a este sitio — lo creamos
                usuario = new UsuarioSitio
                {
                    Auth0Id = auth0User.Sub,
                    Nombre = auth0User.Name ?? auth0User.Email,
                    Email = auth0User.Email,
                    SitioId = request.SitioId,
                    Activo = true,
                    FechaRegistro = DateTime.UtcNow
                };
                _context.UsuariosSitio.Add(usuario);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Actualizamos datos que pueden haber cambiado en Google
                usuario.Nombre = auth0User.Name ?? usuario.Nombre;
                await _context.SaveChangesAsync();
            }

            // 4. Generar JWT propio
            var jwt = GenerarJwt(usuario);

            return new SocialLoginResponse
            {
                Jwt = jwt,
                UsuarioSitioId = usuario.Id,
                SitioId = usuario.SitioId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
            };
        }

        /// <summary>
        /// Guarda o actualiza el token FCM del dispositivo del usuario.
        /// </summary>
        public async Task<bool> GuardarFcmTokenAsync(int usuarioSitioId, string fcmToken)
        {
            var usuario = await _context.UsuariosSitio.FindAsync(usuarioSitioId);
            if (usuario == null) return false;

            usuario.FcmToken = fcmToken;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Valida el token de Auth0 llamando al endpoint userinfo.
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

        /// <summary>
        /// Genera un JWT propio de la plataforma para el UsuarioSitio.
        /// </summary>
        private string GenerarJwt(UsuarioSitio usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Name, usuario.Nombre),
                    new Claim("sitioId", usuario.SitioId.ToString()),
                    new Claim(ClaimTypes.Role, usuario.Rol.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }
    }

    /// <summary>
    /// Modelo interno para deserializar la respuesta de Auth0 /userinfo.
    /// </summary>
    public class Auth0UserInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

    }
}
