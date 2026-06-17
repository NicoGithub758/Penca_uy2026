using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Filters;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    /// <summary>
    /// Panel del AdminSitio para gestionar:
    ///   - Configuracion de recordatorios automaticos (toggle + horas antes)
    ///   - Envio de anuncios inmediatos a participantes de una penca
    /// 
    /// AUTH: Usa el mismo mecanismo que AdminSitioAuthController:
    /// cookie "SitioId_Admin" + filtro [AdminSitioAuthorize].
    /// </summary>
    [Route("AdminSitioAuth/Recordatorios")]
    [AdminSitioAuthorize]
    public class RecordatoriosController : Controller
    {
        private readonly MyDbContext _context;
        private readonly FirebaseNotificationService _firebaseService;
        private readonly ILogger<RecordatoriosController> _logger;

        public RecordatoriosController(
            MyDbContext context,
            FirebaseNotificationService firebaseService,
            ILogger<RecordatoriosController> logger)
        {
            _context = context;
            _firebaseService = firebaseService;
            _logger = logger;
        }

        // ====================================================================
        //  PANTALLA PRINCIPAL
        // ====================================================================

        /// <summary>
        /// Muestra el panel con la config y el formulario para enviar anuncio.
        /// GET /AdminSitioAuth/Recordatorios
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var sitioId = ObtenerSitioId();
            if (sitioId == null) return RedirectToAction("Login", "AdminSitioAuth");

            // Cargar config (crearla con defaults si no existe)
            var config = await ObtenerOCrearConfigAsync(sitioId.Value);

            // Modelos para los 2 formularios de la pantalla
            ViewBag.Configuracion = new ConfiguracionSitioViewModel
            {
                RecordatoriosAutomaticosActivos = config.RecordatoriosAutomaticosActivos,
                HorasAntes = config.HorasAntes
            };

            ViewBag.Anuncio = new EnviarAnuncioViewModel
            {
                PencasDisponibles = await CargarPencasDisponiblesAsync(sitioId.Value)
            };

            return View();
        }

        // ====================================================================
        //  GUARDAR CONFIGURACION
        // ====================================================================

        /// <summary>
        /// Guarda los cambios de la config (toggle + horas antes).
        /// POST /AdminSitioAuth/Recordatorios/GuardarConfiguracion
        /// </summary>
        [HttpPost("GuardarConfiguracion")]
        public async Task<IActionResult> GuardarConfiguracion(ConfiguracionSitioViewModel model)
        {
            var sitioId = ObtenerSitioId();
            if (sitioId == null) return RedirectToAction("Login", "AdminSitioAuth");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Datos inválidos. Verificá los campos.";
                return RedirectToAction(nameof(Index));
            }

            var config = await ObtenerOCrearConfigAsync(sitioId.Value);
            config.RecordatoriosAutomaticosActivos = model.RecordatoriosAutomaticosActivos;
            config.HorasAntes = model.HorasAntes;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"[ConfigSitio] Sitio {sitioId} actualizado: " +
                $"activo={model.RecordatoriosAutomaticosActivos}, horas={model.HorasAntes}");

            TempData["Success"] = "Configuración guardada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ====================================================================
        //  ENVIAR ANUNCIO INMEDIATO
        // ====================================================================

        /// <summary>
        /// Envia una notificacion push inmediata a todos los participantes
        /// de una PencaInstancia.
        /// POST /AdminSitioAuth/Recordatorios/EnviarAnuncio
        /// </summary>
        [HttpPost("EnviarAnuncio")]
        public async Task<IActionResult> EnviarAnuncio(EnviarAnuncioViewModel model)
        {
            var sitioId = ObtenerSitioId();
            if (sitioId == null) return RedirectToAction("Login", "AdminSitioAuth");

            // Validar que la PencaInstancia pertenece al sitio
            var instanciaExiste = await _context.PencaInstancias
                .AnyAsync(pi => pi.Id == model.PencaInstanciaId && pi.SitioId == sitioId.Value);

            if (!instanciaExiste)
            {
                ModelState.AddModelError("PencaInstanciaId", "Penca no válida.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Datos inválidos. Verificá los campos.";
                return RedirectToAction(nameof(Index));
            }

            // Buscar participantes (pagados) de esa instancia
            var usuarioIds = await _context.Participaciones
                .Where(p => p.PencaInstanciaId == model.PencaInstanciaId && p.EstaPagado)
                .Select(p => p.UsuarioSitioId)
                .Distinct()
                .ToListAsync();

            if (!usuarioIds.Any())
            {
                TempData["Error"] = "Esta penca no tiene participantes pagos. No se envió nada.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                int enviadas = await _firebaseService.EnviarNotificacionAMultiplesUsuariosAsync(
                    usuarioSitioIds: usuarioIds,
                    tipo: TipoNotificacion.Generales,
                    titulo: model.Titulo,
                    mensaje: model.Mensaje,
                    data: new Dictionary<string, string>
                    {
                        { "tipo", "anuncio_general" }
                    }
                );

                _logger.LogInformation(
                    $"[Anuncio] Sitio {sitioId} envio anuncio a penca {model.PencaInstanciaId}: " +
                    $"{enviadas}/{usuarioIds.Count} entregados.");

                TempData["Success"] = $"Anuncio enviado a {enviadas} usuario(s) (de {usuarioIds.Count} participantes).";
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Anuncio] Error: {ex.Message}");
                TempData["Error"] = "Hubo un problema al enviar el anuncio. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ====================================================================
        //  HELPERS
        // ====================================================================

        /// <summary>
        /// Lee el SitioId de la cookie "SitioId_Admin" que setea AdminSitioAuthController
        /// al hacer login.
        /// </summary>
        private int? ObtenerSitioId()
        {
            if (!Request.Cookies.TryGetValue("SitioId_Admin", out string? sitioIdStr))
                return null;

            return int.TryParse(sitioIdStr, out int id) ? id : null;
        }

        /// <summary>
        /// Obtiene la config del sitio actual. Si no existe, la crea con valores default.
        /// </summary>
        private async Task<ConfiguracionSitio> ObtenerOCrearConfigAsync(int sitioId)
        {
            var config = await _context.ConfiguracionesSitio
                .FirstOrDefaultAsync(c => c.SitioId == sitioId);

            if (config == null)
            {
                config = new ConfiguracionSitio
                {
                    SitioId = sitioId,
                    RecordatoriosAutomaticosActivos = true,
                    HorasAntes = 1
                };
                _context.ConfiguracionesSitio.Add(config);
                await _context.SaveChangesAsync();
            }

            return config;
        }

        /// <summary>
        /// Carga las pencas del sitio para el dropdown de anuncios.
        /// </summary>
        private async Task<List<PencaOptionViewModel>> CargarPencasDisponiblesAsync(int sitioId)
        {
            return await _context.PencaInstancias
                .Include(pi => pi.Penca)
                .Where(pi => pi.SitioId == sitioId)
                .Select(pi => new PencaOptionViewModel
                {
                    Id = pi.Id,
                    Nombre = pi.Penca.Nombre
                })
                .ToListAsync();
        }
    }
}

