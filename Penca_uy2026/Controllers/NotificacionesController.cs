using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Services;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/notificaciones")]
    [Authorize(Roles = "Jugador")]
    public class NotificacionesController : ControllerBase
    {
        private readonly FirebaseNotificationService _firebaseService;
        private readonly MyDbContext _context;

        public NotificacionesController(FirebaseNotificationService firebaseService, MyDbContext context)
        {
            _firebaseService = firebaseService;
            _context = context;
        }

        /// <summary>
        /// Envía una notificación de prueba al usuario autenticado.
        /// POST /api/notificaciones/prueba
        /// </summary>
        [HttpPost("prueba")]
        public async Task<IActionResult> EnviarNotificacionPrueba()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdClaim, out int usuarioSitioId))
                return Unauthorized();

            var usuario = await _context.UsuariosSitio
                .FirstOrDefaultAsync(u => u.Id == usuarioSitioId);

            if (usuario == null || string.IsNullOrEmpty(usuario.FcmToken))
                return BadRequest("No se encontró el token FCM del usuario.");

            var enviado = await _firebaseService.EnviarNotificacionAsync(
                usuario.FcmToken,
                "Notificación de Prueba 🎉",
                "¡Tu sistema de notificaciones funciona correctamente!",
                new Dictionary<string, string>
                {
                    { "tipo", "prueba" },
                    { "timestamp", DateTime.UtcNow.ToString() }
                }
            );

            if (enviado)
                return Ok(new { mensaje = "Notificación enviada exitosamente" });
            else
                return StatusCode(500, "Error al enviar la notificación");
        }

        /// <summary>
        /// Notifica a todos los participantes de una penca cuando se confirma un resultado.
        /// POST /api/notificaciones/resultado-confirmado
        /// </summary>
        [HttpPost("resultado-confirmado")]
        [Authorize(Roles = "AdminSitio")]
        public async Task<IActionResult> NotificarResultadoConfirmado([FromBody] NotificarResultadoRequest request)
        {
            // Obtener todos los participantes de la penca con token FCM
            var participantes = await _context.Participaciones
                .Include(p => p.UsuarioSitio)
                .Where(p => p.PencaInstanciaId == request.PencaInstanciaId && 
                           !string.IsNullOrEmpty(p.UsuarioSitio.FcmToken))
                .ToListAsync();

            if (!participantes.Any())
                return Ok(new { mensaje = "No hay participantes con notificaciones habilitadas" });

            var tokens = participantes.Select(p => p.UsuarioSitio.FcmToken!).ToList();

            var enviados = await _firebaseService.EnviarNotificacionMultipleAsync(
                tokens,
                "¡Resultado confirmado! ⚽",
                request.Mensaje,
                new Dictionary<string, string>
                {
                    { "tipo", "resultado_confirmado" },
                    { "pencaInstanciaId", request.PencaInstanciaId.ToString() }
                }
            );

            return Ok(new { 
                mensaje = $"Notificaciones enviadas a {enviados} de {tokens.Count} participantes" 
            });
        }
    }

    public class NotificarResultadoRequest
    {
        public int PencaInstanciaId { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
