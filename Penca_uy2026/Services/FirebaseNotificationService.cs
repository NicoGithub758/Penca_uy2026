using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace Penca_uy2026.Services
{
    public class FirebaseNotificationService
    {
        private readonly ILogger<FirebaseNotificationService> _logger;
        private readonly FirebaseApp _firebaseApp;

        public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
        {
            _logger = logger;

            // Inicializar Firebase Admin SDK con el archivo de credenciales
            if (FirebaseApp.DefaultInstance == null)
            {
                _firebaseApp = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile("firebase-adminsdk.json")
                });
            }
            else
            {
                _firebaseApp = FirebaseApp.DefaultInstance;
            }
        }

        /// <summary>
        /// Envía una notificación push a un dispositivo específico.
        /// </summary>
        public async Task<bool> EnviarNotificacionAsync(string fcmToken, string titulo, string mensaje, Dictionary<string, string>? data = null)
        {
            try
            {
                var message = new Message
                {
                    Token = fcmToken,
                    Notification = new Notification
                    {
                        Title = titulo,
                        Body = mensaje
                    },
                    Data = data ?? new Dictionary<string, string>()
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation($"[FCM] Notificación enviada exitosamente: {response}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FCM] Error al enviar notificación: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía una notificación push a múltiples dispositivos.
        /// </summary>
        public async Task<int> EnviarNotificacionMultipleAsync(List<string> fcmTokens, string titulo, string mensaje, Dictionary<string, string>? data = null)
        {
            if (fcmTokens == null || !fcmTokens.Any())
                return 0;

            var message = new MulticastMessage
            {
                Tokens = fcmTokens,
                Notification = new Notification
                {
                    Title = titulo,
                    Body = mensaje
                },
                Data = data ?? new Dictionary<string, string>()
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation($"[FCM] Notificaciones enviadas: {response.SuccessCount} exitosas, {response.FailureCount} fallidas");
                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FCM] Error al enviar notificaciones múltiples: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Envía notificación a todos los participantes de una penca.
        /// </summary>
        public async Task NotificarParticipantesPencaAsync(int pencaInstanciaId, string titulo, string mensaje)
        {
            // Este método se puede extender con inyección del DbContext para buscar tokens
            _logger.LogInformation($"[FCM] Solicitud de notificación para penca {pencaInstanciaId}: {titulo}");
        }
    }
}
