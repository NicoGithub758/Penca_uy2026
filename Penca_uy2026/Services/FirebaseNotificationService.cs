using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Penca_uy2026.Data;

namespace Penca_uy2026.Services
{
    /// <summary>
    /// Tipos de notificaciones que se pueden enviar.
    /// Cada tipo se chequea contra las preferencias del usuario antes de enviar.
    /// </summary>
    public enum TipoNotificacion
    {
        Resultados,
        Partidos,
        Generales,
        Ranking
    }

    public class FirebaseNotificationService
    {
        private readonly ILogger<FirebaseNotificationService> _logger;
        private readonly FirebaseApp _firebaseApp;
        private readonly MyDbContext _context;

        public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger, MyDbContext context)
        {
            _logger = logger;
            _context = context;

            // Inicializar Firebase Admin SDK
            // Soporta dos modos:
            //   - Produccion (Railway): credenciales como JSON en variable de entorno FIREBASE_CREDENTIALS_JSON
            //   - Desarrollo local: archivo firebase-adminsdk.json en la raiz del proyecto
            if (FirebaseApp.DefaultInstance == null)
            {
                GoogleCredential credential = ObtenerCredenciales();

                _firebaseApp = FirebaseApp.Create(new AppOptions
                {
                    Credential = credential
                });

                _logger.LogInformation("[FCM] Firebase Admin SDK inicializado correctamente.");
            }
            else
            {
                _firebaseApp = FirebaseApp.DefaultInstance;
            }
        }

        /// <summary>
        /// Obtiene las credenciales de Firebase. Primero intenta de variable de entorno
        /// (produccion), si no la encuentra usa el archivo local (desarrollo).
        /// </summary>
        private GoogleCredential ObtenerCredenciales()
        {
            // Intento 1: variable de entorno (Railway / produccion)
            var credentialsJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");
            if (!string.IsNullOrEmpty(credentialsJson))
            {
                _logger.LogInformation("[FCM] Cargando credenciales desde variable de entorno FIREBASE_CREDENTIALS_JSON");
                try
                {
                    return GoogleCredential.FromJson(credentialsJson);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[FCM] Error al parsear FIREBASE_CREDENTIALS_JSON: {ex.Message}");
                    throw new InvalidOperationException(
                        "La variable de entorno FIREBASE_CREDENTIALS_JSON existe pero no es un JSON valido.", ex);
                }
            }

            // Intento 2: archivo local (desarrollo)
            const string archivoLocal = "firebase-adminsdk.json";
            if (File.Exists(archivoLocal))
            {
                _logger.LogInformation($"[FCM] Cargando credenciales desde archivo local: {archivoLocal}");
                return GoogleCredential.FromFile(archivoLocal);
            }

            throw new InvalidOperationException(
                "No se encontraron credenciales de Firebase. " +
                "En produccion configurar la variable de entorno FIREBASE_CREDENTIALS_JSON. " +
                "En desarrollo colocar el archivo firebase-adminsdk.json en la raiz del proyecto.");
        }

        // ====================================================================
        //  ENVIO CON CHEQUEO DE PREFERENCIAS (METODOS RECOMENDADOS)
        // ====================================================================

        /// <summary>
        /// Envia una notificacion a un usuario especifico, respetando sus preferencias.
        /// Si el usuario tiene desactivado el tipo de notificacion, NO se envia.
        /// </summary>
        public async Task<bool> EnviarNotificacionAUsuarioAsync(
            int usuarioSitioId,
            TipoNotificacion tipo,
            string titulo,
            string mensaje,
            Dictionary<string, string>? data = null)
        {
            // 1. Verificar preferencias del usuario
            var prefs = await _context.PreferenciasNotificacion
                .FirstOrDefaultAsync(p => p.UsuarioSitioId == usuarioSitioId);

            // Si no hay preferencias guardadas, asumimos que las quiere todas (default)
            if (prefs != null && !PermitiTipo(prefs, tipo))
            {
                _logger.LogInformation($"[FCM] Usuario {usuarioSitioId} tiene desactivadas las notificaciones de tipo {tipo}");
                return false;
            }

            // 2. Buscar el token FCM del usuario
            var usuario = await _context.UsuariosSitio
                .FirstOrDefaultAsync(u => u.Id == usuarioSitioId);

            if (usuario == null || string.IsNullOrEmpty(usuario.FcmToken))
            {
                _logger.LogInformation($"[FCM] Usuario {usuarioSitioId} no tiene token FCM");
                return false;
            }

            // 3. Enviar
            return await EnviarNotificacionAsync(usuario.FcmToken, titulo, mensaje, data);
        }

        /// <summary>
        /// Envia una notificacion a multiples usuarios, respetando las preferencias de cada uno.
        /// Filtra automaticamente los que tienen ese tipo desactivado.
        /// </summary>
        public async Task<int> EnviarNotificacionAMultiplesUsuariosAsync(
            List<int> usuarioSitioIds,
            TipoNotificacion tipo,
            string titulo,
            string mensaje,
            Dictionary<string, string>? data = null)
        {
            if (usuarioSitioIds == null || !usuarioSitioIds.Any()) return 0;

            // 1. Traer todas las preferencias relevantes en UNA consulta
            var prefsPorUsuario = await _context.PreferenciasNotificacion
                .Where(p => usuarioSitioIds.Contains(p.UsuarioSitioId))
                .ToDictionaryAsync(p => p.UsuarioSitioId);

            // 2. Filtrar usuarios que aceptan este tipo
            //    Si el usuario NO tiene preferencias guardadas, asumimos true (default)
            var usuariosQueAceptan = usuarioSitioIds
                .Where(id => !prefsPorUsuario.ContainsKey(id) || PermitiTipo(prefsPorUsuario[id], tipo))
                .ToList();

            if (!usuariosQueAceptan.Any())
            {
                _logger.LogInformation($"[FCM] Ningun usuario acepta notificaciones de tipo {tipo}");
                return 0;
            }

            // 3. Buscar tokens FCM de los usuarios que aceptan
            var tokens = await _context.UsuariosSitio
                .Where(u => usuariosQueAceptan.Contains(u.Id) && !string.IsNullOrEmpty(u.FcmToken))
                .Select(u => u.FcmToken!)
                .ToListAsync();

            if (!tokens.Any()) return 0;

            // 4. Enviar
            return await EnviarNotificacionMultipleAsync(tokens, titulo, mensaje, data);
        }

        /// <summary>
        /// Determina si las preferencias permiten un tipo de notificacion.
        /// </summary>
        private bool PermitiTipo(Models.PreferenciaNotificacion prefs, TipoNotificacion tipo)
        {
            return tipo switch
            {
                TipoNotificacion.Resultados => prefs.RecibirResultados,
                TipoNotificacion.Partidos => prefs.RecibirPartidos,
                TipoNotificacion.Generales => prefs.RecibirGenerales,
                TipoNotificacion.Ranking => prefs.RecibirRanking,
                _ => true
            };
        }

        // ====================================================================
        //  METODOS DE BAJO NIVEL (sin filtrado de preferencias)
        //  Usalos solo si ya filtraste antes, o para notificaciones de testing.
        // ====================================================================

        /// <summary>
        /// Envia una notificacion push a un dispositivo especifico SIN chequear preferencias.
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
                _logger.LogInformation($"[FCM] Notificacion enviada: {response}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FCM] Error al enviar notificacion: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envia una notificacion push a multiples dispositivos SIN chequear preferencias.
        /// </summary>
        public async Task<int> EnviarNotificacionMultipleAsync(List<string> fcmTokens, string titulo, string mensaje, Dictionary<string, string>? data = null)
        {
            if (fcmTokens == null || !fcmTokens.Any()) return 0;

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
                _logger.LogInformation($"[FCM] {response.SuccessCount} exitosas, {response.FailureCount} fallidas");
                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FCM] Error al enviar notificaciones multiples: {ex.Message}");
                return 0;
            }
        }
    }
}

