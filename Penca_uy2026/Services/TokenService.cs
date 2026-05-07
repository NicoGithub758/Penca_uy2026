using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Penca_uy2026.Models;

namespace Penca_uy2026.Services
{
    /// <summary>
    /// Servicio centralizado para la gestión y generación de JSON Web Tokens (JWT).
    /// </summary>
    public class TokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Genera un token JWT firmado para un usuario específico del sitio.
        /// El token incluye información básica de identidad y el rol del usuario.
        /// </summary>
        /// <param name="usuario">Entidad del usuario para el cual se generará el token.</param>
        /// <returns>Cadena de texto con el token JWT codificado.</returns>
        public string GenerarJwtParaUsuario(UsuarioSitio usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Se obtiene la clave secreta desde la configuración del sistema.
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            // Se definen las declaraciones (Claims) que formarán la carga útil del token.
            // Estas permiten identificar al usuario en peticiones futuras sin consultar la base de datos.
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
                // Se establece un tiempo de vida de 7 días para la sesión web.
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            // Se crea y se escribe el token final.
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
