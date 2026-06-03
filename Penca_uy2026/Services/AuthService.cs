using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Penca_uy2026.Data;
using Penca_uy2026.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Penca_uy2026.Services
{
    public class AuthService
    {
        private readonly MyDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(MyDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public string? LoginPlataforma(LoginRequest request)
        {
            var admin = _context.PlataformaAdmins.FirstOrDefault(a => a.Email == request.Email);
            if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Email, admin.Email),
                    new Claim(ClaimTypes.Role, "PlataformaAdmin")
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }
        public async Task<UsuarioSitio?> ValidarAdminSitioAsync(string email, string password)
        {
            // Buscamos al usuario por email y rol de admin
            var usuario = await _context.UsuariosSitio
                .FirstOrDefaultAsync<UsuarioSitio>(u => u.Email.ToLower() == email.ToLower()
                                                   && u.Rol == RolUsuarioSitio.AdminSitio);

            if (usuario == null || string.IsNullOrEmpty(usuario.PasswordHash)) return null;

            bool esValido = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

            return esValido ? usuario : null;
        }
    }
}