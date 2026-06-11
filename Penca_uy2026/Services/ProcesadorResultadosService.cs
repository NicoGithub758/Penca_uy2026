using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Hubs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Penca_uy2026.Services
{
    public class ProcesadorResultadosService
    {
        private readonly MyDbContext _context;
        private readonly IHubContext<PencaHub> _hubContext;

        public ProcesadorResultadosService(MyDbContext context, IHubContext<PencaHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task ProcesarPartidoAsync(int partidoId)
        {
            // 1. Obtener el partido ya actualizado (ignora filtros de tenant porque esto corre como un proceso global a veces)
            var partido = await _context.Partidos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == partidoId);
            if (partido == null || !partido.Jugado) return; // Si no existe o no se ha jugado, no hacemos nada

            // 2. Traer todas las predicciones asociadas a este partido
            var predicciones = await _context.Predicciones
                .IgnoreQueryFilters()
                .Include(p => p.Participacion)
                .Where(p => p.PartidoId == partidoId)
                .ToListAsync();

            if (!predicciones.Any()) return;

            // 3. PASO IDEMPOTENTE 1: Calcular/Sobreescribir Puntos Obtenidos de la predicción
            foreach (var prediccion in predicciones)
            {
                int puntos = CalcularPuntos(
                    golesLocalReal: partido.GolesLocal ?? 0,
                    golesVisitanteReal: partido.GolesVisitante ?? 0,
                    golesLocalPredichos: prediccion.GolesEquipoLocal,
                    golesVisitantePredichos: prediccion.GolesEquipoVisitante
                );

                prediccion.PuntosObtenidos = puntos;
            }

            // Guardamos los puntos de las predicciones primero
            await _context.SaveChangesAsync();

            // 4. PASO IDEMPOTENTE 2: Recalcular PuntajeTotal de todas las participaciones afectadas
            var participacionesAfectadas = predicciones.Select(p => p.Participacion).Distinct().ToList();

            foreach (var participacion in participacionesAfectadas)
            {
                // Sumar desde cero todas las predicciones de esta participación
                var sumaTotal = await _context.Predicciones
                    .IgnoreQueryFilters()
                    .Where(p => p.ParticipacionId == participacion.Id)
                    .SumAsync(p => p.PuntosObtenidos);

                participacion.PuntajeTotal = sumaTotal;
            }

            // Guardamos el puntaje total recalculado
            await _context.SaveChangesAsync();

            // 5. NOTIFICAR VÍA SIGNALR
            // Notificamos a las salas específicas de las Instancias afectadas para que React actualice
            var instanciasIds = participacionesAfectadas.Select(p => p.PencaInstanciaId).Distinct();
            foreach (var instanciaId in instanciasIds)
            {
                await _hubContext.Clients.Group($"penca-{instanciaId}").SendAsync("PencaUpdated");
            }
        }

        private int CalcularPuntos(int golesLocalReal, int golesVisitanteReal, int golesLocalPredichos, int golesVisitantePredichos)
        {
            // Exacto: Acertó exactamente los goles
            if (golesLocalReal == golesLocalPredichos && golesVisitanteReal == golesVisitantePredichos)
            {
                return 10;
            }

            // Determinar ganadores reales
            bool ganoLocalReal = golesLocalReal > golesVisitanteReal;
            bool ganoVisitanteReal = golesVisitanteReal > golesLocalReal;
            bool empateReal = golesLocalReal == golesVisitanteReal;

            // Determinar ganadores predichos
            bool ganoLocalPredicho = golesLocalPredichos > golesVisitantePredichos;
            bool ganoVisitantePredicho = golesVisitantePredichos > golesLocalPredichos;
            bool empatePredicho = golesLocalPredichos == golesVisitantePredichos;

            // Tendencia: Acertó quién ganaba o si empataban, pero erró en los goles exactos
            if ((ganoLocalReal && ganoLocalPredicho) ||
                (ganoVisitanteReal && ganoVisitantePredicho) ||
                (empateReal && empatePredicho))
            {
                return 5;
            }

            // Error: No le pegó ni al resultado ni a la tendencia
            return 0;
        }
    }
}
