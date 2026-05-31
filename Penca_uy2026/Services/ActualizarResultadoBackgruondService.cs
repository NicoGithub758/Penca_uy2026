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
            _logger.LogInformation("Actualizador automatico de resultados iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var intervaloMinutos = 10;

                try
                {
                    _logger.LogInformation("Iniciando ciclo de actualizacion automatica de resultados.");

                    using var scope = _scopeFactory.CreateScope();

                    var parametrosService = scope.ServiceProvider
                        .GetRequiredService<ParametrosSistemaService>();

                    var parametros = await parametrosService.ObtenerAsync(stoppingToken);
                    intervaloMinutos = Math.Max(1, parametros.IntervaloMinutosConsultaResultados);

                    var service = scope.ServiceProvider
                        .GetRequiredService<ActualizarResultadosService>();

                    await service.ActualizarResultadosAsync(stoppingToken);

                    _logger.LogInformation(
                        "Finalizo ciclo de actualizacion automatica de resultados. Proximo ciclo en {IntervaloMinutos} minutos.",
                        intervaloMinutos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error actualizando resultados desde API-Football.");
                }

                await Task.Delay(TimeSpan.FromMinutes(intervaloMinutos), stoppingToken);
            }
        }
    }
}
