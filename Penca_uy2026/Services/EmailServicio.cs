using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Penca_uy2026.Interfaces;

namespace Penca_uy2026.Services
{
    public class EmailServicio : IEmailServicio
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EmailServicio(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task EnviarEmailInvitacionAsync(string emailDestino, string nombreAdmin, string tokenInvitacion, string urlSitio)
        {
            var emailEmisor = _configuration["EmailSettings:SenderEmail"]?.Trim();
            var smtpPassword = _configuration["EmailSettings:SenderPassword"]?.Trim(); // Acá vas a poner la contraseña SMTP vieja (la xsmtpsib-)

            if (string.IsNullOrEmpty(emailEmisor) || string.IsNullOrEmpty(smtpPassword))
            {
                Console.WriteLine("--- [ALERTA EMAIL NO CONFIGURADO] ---");
                return;
            }

            string linkActivacion = $"https://{urlSitio}/AdminAuth/ConfigurarPassword?token={tokenInvitacion}";

            var payload = new
            {
                sender = new { email = emailEmisor, name = "Plataforma Penca UY" },
                to = new[] { new { email = emailDestino.Trim(), name = nombreAdmin.Trim() } },
                subject = "🔑 Activación de tu cuenta de Administrador",
                htmlContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                        <h2 style='color: #212529; text-align: center;'>¡Bienvenido a tu nueva Penca!</h2>
                        <p>Hola <strong>{nombreAdmin}</strong>,</p>
                        <p>Se ha registrado un nuevo sitio para ti en nuestra plataforma.</p>
                        <p>Para activar tu cuenta de administrador y configurar tu contraseña de acceso, haz clic en el siguiente botón:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{linkActivacion}' style='background-color: #0d6efd; color: white; padding: 12px 25px; text-decoration: none; font-weight: bold; border-radius: 5px; display: inline-block;'>Configurar mi Contraseña</a>
                        </div>
                        <p style='color: #6c757d; font-size: 12px;'>Si el botón no funciona, copia el enlace:<br>{linkActivacion}</p>
                    </div>"
            };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");

                // 🔑 TRUCO MAESTRO: Autenticación básica (usuario:password) convertida a Base64
                // Usamos el mail como usuario y la clave SMTP como password
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{emailEmisor}:{smtpPassword}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                request.Headers.Add("accept", "application/json");

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"=== [EMAIL ENVIADO VIA HTTP-BASIC EXITOSAMENTE] a {emailDestino} ===");
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"=== [ERROR API BREVO]: Status {response.StatusCode} - {errorBody} ===");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== [ERROR CRITICO HTTP EMAIL]: {ex.Message} ===");
            }
        }
    }
}