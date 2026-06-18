using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Hubs;
using Penca_uy2026.Models;

namespace Penca_uy2026.Services
{
    public class ProcesadorResultadosService
    {
        private readonly MyDbContext _context;
        private readonly IHubContext<PencaHub> _hubContext;
        private readonly ParametrosSistemaService _parametrosSistemaService;

        public ProcesadorResultadosService(
            MyDbContext context,
            IHubContext<PencaHub> hubContext,
            ParametrosSistemaService parametrosSistemaService)
        {
            _context = context;
            _hubContext = hubContext;
            _parametrosSistemaService = parametrosSistemaService;
        }

        public async Task ProcesarPartidoAsync(int partidoId)
        {
            var partido = await _context.Partidos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == partidoId);

            if (partido == null || !partido.Jugado)
                return;

            var predicciones = await _context.Predicciones
                .IgnoreQueryFilters()
                .Include(p => p.Participacion)
                .Where(p => p.PartidoId == partidoId)
                .ToListAsync();

            if (!predicciones.Any())
                return;

            var parametros = await _parametrosSistemaService.ObtenerAsync();

            foreach (var prediccion in predicciones)
            {
                var puntos = CalcularPuntos(
                    golesLocalReal: partido.GolesLocal ?? 0,
                    golesVisitanteReal: partido.GolesVisitante ?? 0,
                    golesLocalPredichos: prediccion.GolesEquipoLocal,
                    golesVisitantePredichos: prediccion.GolesEquipoVisitante,
                    parametros: parametros
                );

                prediccion.PuntosObtenidos = puntos;
            }

            await _context.SaveChangesAsync();

            var participacionesAfectadas = predicciones
                .Select(p => p.Participacion)
                .Distinct()
                .ToList();

            foreach (var participacion in participacionesAfectadas)
            {
                var sumaTotal = await _context.Predicciones
                    .IgnoreQueryFilters()
                    .Where(p => p.ParticipacionId == participacion.Id)
                    .SumAsync(p => p.PuntosObtenidos);

                participacion.PuntajeTotal = sumaTotal;
            }

            await _context.SaveChangesAsync();

            var instanciasIds = participacionesAfectadas
                .Select(p => p.PencaInstanciaId)
                .Distinct();

            foreach (var instanciaId in instanciasIds)
            {
                await _hubContext.Clients.Group($"penca-{instanciaId}").SendAsync("PencaUpdated");
            }
        }

        private int CalcularPuntos(
            int golesLocalReal,
            int golesVisitanteReal,
            int golesLocalPredichos,
            int golesVisitantePredichos,
            ParametrosSistema parametros)
        {
            if (golesLocalReal == golesLocalPredichos && golesVisitanteReal == golesVisitantePredichos)
            {
                return parametros.PuntosResultadoExacto;
            }

            var diferenciaReal = golesLocalReal - golesVisitanteReal;
            var diferenciaPredicha = golesLocalPredichos - golesVisitantePredichos;

            var empateReal = diferenciaReal == 0;
            var empatePredicho = diferenciaPredicha == 0;

            var acertoGanadorOEmpate =
                (diferenciaReal > 0 && diferenciaPredicha > 0) ||
                (diferenciaReal < 0 && diferenciaPredicha < 0) ||
                (empateReal && empatePredicho);

            if (!acertoGanadorOEmpate)
            {
                return 0;
            }

            if (!empateReal && diferenciaReal == diferenciaPredicha)
            {
                return parametros.PuntosGanadorDiferenciaGoles;
            }

            return parametros.PuntosGanadorEmpate;
        }
    }
}
