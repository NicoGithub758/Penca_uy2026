namespace Penca_uy2026.Services
{
    public class ActualizarResultadosBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ActualizarResultadosBackgroundService> _logger;

        public ActualizarResultadosBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ActualizarResultadosBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));

            _logger.LogInformation("Actualizador automatico de resultados iniciado. Intervalo: 10 minutos.");

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Iniciando ciclo de actualizacion automatica de resultados.");

                    using var scope = _scopeFactory.CreateScope();

                    var service = scope.ServiceProvider
                        .GetRequiredService<ActualizarResultadosService>();

                    await service.ActualizarResultadosAsync(stoppingToken);

                    _logger.LogInformation("Finalizo ciclo de actualizacion automatica de resultados.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error actualizando resultados desde API-Football.");
                }
            }
        }
    }
}
