using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Penca_uy2026.Interfaces;

namespace Penca_uy2026.Services
{
    public class EmailServicio : IEmailServicio
    {
        private readonly IConfiguration _configuration;

        public EmailServicio(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarEmailInvitacionAsync(string emailDestino, string nombreAdmin, string tokenInvitacion, string urlSitio)
        {
            // 1. Leemos las credenciales desde la configuración (Local o Railway)
            var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var emailEmisor = _configuration["EmailSettings:SenderEmail"];
            var passwordEmisor = _configuration["EmailSettings:SenderPassword"]; // Contraseña de aplicación

            if (string.IsNullOrEmpty(emailEmisor) || string.IsNullOrEmpty(passwordEmisor))
            {
                // Si no configuraste el mail todavía, lo tiramos a la consola de Railway para poder debuguear el token sin trancar el flujo
                Console.WriteLine($"--- [ALERTA EMAIL NO CONFIGURADO] ---");
                Console.WriteLine($"Token para {emailDestino}: {tokenInvitacion}");
                return;
            }

            // 2. Armamos el link definitivo. Apunta al endpoint GET que creamos recién
            // En producción 'urlSitio' será "tupenca.uy" o tu dominio en Railway
            string linkActivacion = $"https://{urlSitio}/AdminAuth/ConfigurarPassword?token={tokenInvitacion}";

            // 3. Diseñamos el cuerpo del Mail (HTML limpio)
            string cuerpoHtml = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                    <h2 style='color: #212529; text-align: center;'>¡Bienvenido a tu nueva Penca!</h2>
                    <p>Hola <strong>{nombreAdmin}</strong>,</p>
                    <p>Se ha registrado un nuevo sitio para ti en nuestra plataforma de pencas deportivas.</p>
                    <p>Para activar tu cuenta de administrador y configurar tu contraseña de acceso de forma segura, haz clic en el siguiente botón (este enlace expira en 48 horas):</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{linkActivacion}' style='background-color: #0d6efd; color: white; padding: 12px 25px; text-decoration: none; font-weight: bold; border-radius: 5px; display: inline-block;'>Configurar mi Contraseña</a>
                    </div>
                    <p style='color: #6c757d; font-size: 12px;'>Si el botón no funciona, copia y pega este enlace en tu navegador:<br>{linkActivacion}</p>
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                    <p style='font-size: 11px; color: #999; text-align: center;'>Equipo de Penca_uy2026</p>
                </div>";

            // 4. Despachamos el correo usando SMTP nativo
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(emailEmisor, passwordEmisor),
                EnableSsl = true
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(emailEmisor, "Plataforma Penca .UY"),
                Subject = "🔑 Activación de tu cuenta de Administrador",
                Body = cuerpoHtml,
                IsBodyHtml = true
            };

            mailMessage.To.Add(emailDestino);

            await client.SendMailAsync(mailMessage);
        }
    }
}