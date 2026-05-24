using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
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
            var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "465");
            var emailEmisor = _configuration["EmailSettings:SenderEmail"];
            var passwordEmisor = _configuration["EmailSettings:SenderPassword"];

            if (string.IsNullOrEmpty(emailEmisor) || string.IsNullOrEmpty(passwordEmisor))
            {
                Console.WriteLine("--- [ALERTA EMAIL NO CONFIGURADO] ---");
                return;
            }

            string linkActivacion = $"https://{urlSitio}/AdminAuth/ConfigurarPassword?token={tokenInvitacion}";

            // 1. Crear el mensaje con MimeKit
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("Plataforma Penca .UY", emailEmisor));
            mensaje.To.Add(new MailboxAddress(nombreAdmin, emailDestino));
            mensaje.Subject = "🔑 Activación de tu cuenta de Administrador";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
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
            mensaje.Body = bodyBuilder.ToMessageBody();

            // 2. Enviar el mensaje usando el cliente de MailKit
            using var client = new SmtpClient();
            try
            {
                // Forzamos el puerto 465 de forma segura
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);

                // Autenticación
                await client.AuthenticateAsync(emailEmisor, passwordEmisor);

                // Envío
                await client.SendAsync(mensaje);

                Console.WriteLine($"=== [EMAIL ENVIADO EXITOSAMENTE] a {emailDestino} ===");
            }
            catch (Exception ex)
            {
                // ESTO NOS VA A DECIR EL ERROR REAL EN RAILWAY
                Console.WriteLine($"=== [ERROR CRITICO SMTP MAILKIT]: {ex.Message} ===");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"=== [INNER EXCEPTION]: {ex.InnerException.Message} ===");
                }
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}