using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;

namespace Penca_uy2026.Services
{
    /// <summary>
    /// Background service que cada X minutos busca partidos que estan por empezar
    /// y envia recordatorios push a los participantes que NO han predicho aun.
    /// 
    /// Respeta la ConfiguracionSitio de cada sitio:
    ///   - Si el sitio tiene RecordatoriosAutomaticosActivos = false, NO se envia nada.
    ///   - HorasAntes determina cuanto antes del partido se notifica.
    /// 
    /// Para detectar "ya notifique este partido en este sitio", usa la tabla
    /// RecordatoriosPartidoSitio (relacion N:N entre Partido y Sitio).
    /// Esto es importante porque Partido es global y cada sitio puede tener
    /// un HorasAntes distinto.
    /// </summary>
    public class RecordatoriosBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RecordatoriosBackgroundService> _logger;

        // Cada cuanto se ejecuta el ciclo
        private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(5);

        public RecordatoriosBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<RecordatoriosBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[Recordatorios] Background service iniciado.");

            // Esperar 30s antes de la primera ejecucion (que la app termine de arrancar)
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarRecordatoriosAutomaticosAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Recordatorios] Error en ciclo: {ex.Message}");
                }

                await Task.Delay(_intervalo, stoppingToken);
            }
        }

        private async Task ProcesarRecordatoriosAutomaticosAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var firebaseService = scope.ServiceProvider.GetRequiredService<FirebaseNotificationService>();

            var ahora = DateTime.UtcNow;

            // Traer todas las configs ACTIVAS (con su HorasAntes)
            var configsActivas = await context.ConfiguracionesSitio
                .IgnoreQueryFilters()
                .Where(c => c.RecordatoriosAutomaticosActivos)
                .ToListAsync(stoppingToken);

            if (!configsActivas.Any())
            {
                _logger.LogInformation("[Recordatorios] Ningun sitio tiene recordatorios automaticos activos.");
                return;
            }

            // Procesar cada sitio activo independientemente
            foreach (var config in configsActivas)
            {
                try
                {
                    await ProcesarSitioAsync(context, firebaseService, config, ahora, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Recordatorios] Error procesando sitio {config.SitioId}: {ex.Message}");
                }
            }

            await context.SaveChangesAsync(stoppingToken);
        }

        private async Task ProcesarSitioAsync(
            MyDbContext context,
            FirebaseNotificationService firebaseService,
            Models.ConfiguracionSitio config,
            DateTime ahora,
            CancellationToken stoppingToken)
        {
            // La ventana del recordatorio es:
            //   desde: (limiteSuperior - intervalo)
            //   hasta: limiteSuperior
            // donde limiteSuperior = ahora + HorasAntes.
            //
            // Por ejemplo, si HorasAntes=1 y el ciclo es cada 5min:
            //   Ventana = [ahora + 55min, ahora + 60min]
            //
            // Asi cada partido entra en exactamente UN ciclo (no se manda 12 veces
            // durante la hora previa).
            var limiteSuperior = ahora.AddHours(config.HorasAntes);
            var limiteInferior = limiteSuperior.Subtract(_intervalo);

            // Buscar partidos en pencas con instancias en este sitio,
            // que NO hayan sido notificados todavia para ESTE sitio especifico.
            var partidosProximos = await context.Partidos
                .IgnoreQueryFilters()
                .Include(p => p.Local)
                .Include(p => p.Visitante)
                .Where(p => !p.Jugado
                            && p.Fecha > limiteInferior
                            && p.Fecha <= limiteSuperior
                            // El partido pertenece a una penca que tiene instancias en este sitio
                            && context.PencaInstancias
                                .IgnoreQueryFilters()
                                .Any(pi => pi.PencaId == p.PencaId && pi.SitioId == config.SitioId)
                            // NO se notifico todavia para este sitio
                            && !context.RecordatoriosPartidoSitio
                                .IgnoreQueryFilters()
                                .Any(rps => rps.PartidoId == p.Id && rps.SitioId == config.SitioId))
                .ToListAsync(stoppingToken);

            if (!partidosProximos.Any()) return;

            _logger.LogInformation(
                $"[Recordatorios] Sitio {config.SitioId}: {partidosProximos.Count} partido(s) en ventana.");

            foreach (var partido in partidosProximos)
            {
                try
                {
                    await NotificarPartidoAsync(context, firebaseService, partido, config.SitioId, ahora, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Recordatorios] Error en partido {partido.Id}: {ex.Message}");
                }
            }
        }

        private async Task NotificarPartidoAsync(
            MyDbContext context,
            FirebaseNotificationService firebaseService,
            Models.Partido partido,
            int sitioId,
            DateTime ahora,
            CancellationToken stoppingToken)
        {
            // Buscar instancias de la penca en este sitio especifico
            var instanciasIds = await context.PencaInstancias
                .IgnoreQueryFilters()
                .Where(pi => pi.PencaId == partido.PencaId && pi.SitioId == sitioId)
                .Select(pi => pi.Id)
                .ToListAsync(stoppingToken);

            // Usuarios participantes (pagados) que NO han predicho este partido
            var usuariosSinPrediccion = await context.Participaciones
                .IgnoreQueryFilters()
                .Where(part => instanciasIds.Contains(part.PencaInstanciaId)
                               && part.EstaPagado
                               && !context.Predicciones
                                   .IgnoreQueryFilters()
                                   .Any(pred => pred.ParticipacionId == part.Id
                                                && pred.PartidoId == partido.Id))
                .Select(part => part.UsuarioSitioId)
                .Distinct()
                .ToListAsync(stoppingToken);

            int enviadas = 0;

            if (usuariosSinPrediccion.Any())
            {
                var minutosRestantes = (int)Math.Ceiling((partido.Fecha - ahora).TotalMinutes);
                var local = partido.Local?.Nombre ?? "Local";
                var visitante = partido.Visitante?.Nombre ?? "Visitante";

                string mensaje = minutosRestantes < 60
                    ? $"{local} vs {visitante} empieza en {minutosRestantes} minutos. ¡Predecí!"
                    : $"{local} vs {visitante} empieza en {minutosRestantes / 60}h. ¡Predecí!";

                enviadas = await firebaseService.EnviarNotificacionAMultiplesUsuariosAsync(
                    usuarioSitioIds: usuariosSinPrediccion,
                    tipo: TipoNotificacion.Partidos,
                    titulo: "⏰ ¡Falta poco!",
                    mensaje: mensaje,
                    data: new Dictionary<string, string>
                    {
                        { "tipo", "recordatorio_partido" },
                        { "partidoId", partido.Id.ToString() }
                    }
                );

                _logger.LogInformation(
                    $"[Recordatorios] Partido {partido.Id} ({local} vs {visitante}) en sitio {sitioId}: " +
                    $"{enviadas}/{usuariosSinPrediccion.Count} recordatorios enviados.");
            }
            else
            {
                _logger.LogInformation(
                    $"[Recordatorios] Partido {partido.Id} en sitio {sitioId}: todos predijeron. " +
                    "Igual se marca como notificado para no reintentarlo.");
            }

            // Registrar que este partido ya se procesó para este sitio
            // (incluso si no hay usuarios para notificar, asi no se reintenta cada 5 min)
            context.RecordatoriosPartidoSitio.Add(new Models.RecordatorioPartidoSitio
            {
                PartidoId = partido.Id,
                SitioId = sitioId,
                FechaEnvio = ahora,
                CantidadEnviados = enviadas
            });
        }
    }
}
