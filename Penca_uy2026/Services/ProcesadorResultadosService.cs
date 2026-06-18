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
        private readonly FirebaseNotificationService _firebaseService;
        private readonly ParametrosSistemaService _parametrosSistemaService;
        private readonly ILogger<ProcesadorResultadosService> _logger;

        public ProcesadorResultadosService(
            MyDbContext context,
            IHubContext<PencaHub> hubContext,
            FirebaseNotificationService firebaseService,
            ParametrosSistemaService parametrosSistemaService,
            ILogger<ProcesadorResultadosService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _firebaseService = firebaseService;
            _parametrosSistemaService = parametrosSistemaService;
            _logger = logger;
        }

        public async Task ProcesarPartidoAsync(int partidoId)
        {
            // 1. Obtener el partido (con equipos para el mensaje de push)
            var partido = await _context.Partidos
                .IgnoreQueryFilters()
                .Include(p => p.Local)
                .Include(p => p.Visitante)
                .FirstOrDefaultAsync(p => p.Id == partidoId);

            if (partido == null || !partido.Jugado) return;

            // 2. Traer todas las predicciones asociadas a este partido
            var predicciones = await _context.Predicciones
                .IgnoreQueryFilters()
                .Include(p => p.Participacion)
                .Where(p => p.PartidoId == partidoId)
                .ToListAsync();

            if (!predicciones.Any()) return;

            // 3. Obtener parametros configurables de puntuacion
            var parametros = await _parametrosSistemaService.ObtenerAsync();

            // 4. PASO IDEMPOTENTE 1: Calcular/Sobreescribir Puntos Obtenidos
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

            // 5. CALCULAR POSICIONES ANTES de actualizar PuntajeTotal
            // Tomamos foto del ranking por cada PencaInstancia afectada
            var participacionesAfectadas = predicciones.Select(p => p.Participacion).Distinct().ToList();
            var instanciasAfectadas = participacionesAfectadas.Select(p => p.PencaInstanciaId).Distinct().ToList();

            var rankingAntes = await CalcularRankingPorInstanciaAsync(instanciasAfectadas);

            // 6. PASO IDEMPOTENTE 2: Recalcular PuntajeTotal de cada participacion
            foreach (var participacion in participacionesAfectadas)
            {
                var sumaTotal = await _context.Predicciones
                    .IgnoreQueryFilters()
                    .Where(p => p.ParticipacionId == participacion.Id)
                    .SumAsync(p => p.PuntosObtenidos);
                participacion.PuntajeTotal = sumaTotal;
            }
            await _context.SaveChangesAsync();

            // 7. CALCULAR POSICIONES DESPUES del recalculo
            var rankingDespues = await CalcularRankingPorInstanciaAsync(instanciasAfectadas);

            // 8. NOTIFICAR VÍA SIGNALR (para el web React en tiempo real)
            foreach (var instanciaId in instanciasAfectadas)
            {
                await _hubContext.Clients.Group($"penca-{instanciaId}").SendAsync("PencaUpdated");
            }

            // 9. NOTIFICAR PUSH: resultado del partido a todos los predictores
            await EnviarNotificacionResultadoAsync(partido, participacionesAfectadas);

            // 10. NOTIFICAR PUSH: cambios de ranking (a los que BAJARON de posicion)
            await EnviarNotificacionRankingAsync(rankingAntes, rankingDespues);
        }

        /// <summary>
        /// Calcula el ranking actual de cada PencaInstancia.
        /// Devuelve diccionario: { instanciaId: { usuarioSitioId: posicion } }
        /// </summary>
        private async Task<Dictionary<int, Dictionary<int, int>>> CalcularRankingPorInstanciaAsync(List<int> instanciaIds)
        {
            var resultado = new Dictionary<int, Dictionary<int, int>>();

            foreach (var instanciaId in instanciaIds)
            {
                // Ordenar participantes por PuntajeTotal descendente
                // En empate, ordenar por UsuarioSitioId para consistencia
                var participantesOrdenados = await _context.Participaciones
                    .IgnoreQueryFilters()
                    .Where(p => p.PencaInstanciaId == instanciaId && p.EstaPagado)
                    .OrderByDescending(p => p.PuntajeTotal)
                    .ThenBy(p => p.UsuarioSitioId)
                    .Select(p => p.UsuarioSitioId)
                    .ToListAsync();

                var posicionesEnEstaInstancia = new Dictionary<int, int>();
                for (int i = 0; i < participantesOrdenados.Count; i++)
                {
                    posicionesEnEstaInstancia[participantesOrdenados[i]] = i + 1; // posiciones 1-based
                }

                resultado[instanciaId] = posicionesEnEstaInstancia;
            }

            return resultado;
        }

        /// <summary>
        /// Compara rankings antes vs despues y notifica a los que BAJARON de posicion.
        /// </summary>
        private async Task EnviarNotificacionRankingAsync(
            Dictionary<int, Dictionary<int, int>> rankingAntes,
            Dictionary<int, Dictionary<int, int>> rankingDespues)
        {
            try
            {
                var usuariosSuperados = new List<int>();

                foreach (var instanciaId in rankingAntes.Keys)
                {
                    var antes = rankingAntes[instanciaId];
                    var despues = rankingDespues[instanciaId];

                    foreach (var usuarioSitioId in antes.Keys)
                    {
                        if (!despues.ContainsKey(usuarioSitioId)) continue;

                        int posicionAntes = antes[usuarioSitioId];
                        int posicionDespues = despues[usuarioSitioId];

                        // Si BAJO de posicion (numero mayor = peor posicion), notificar
                        // Ej: estaba 1ro (posAntes=1) y ahora esta 2do (posDespues=2)
                        if (posicionDespues > posicionAntes)
                        {
                            usuariosSuperados.Add(usuarioSitioId);
                        }
                    }
                }

                if (!usuariosSuperados.Any())
                {
                    _logger.LogInformation("[Ranking] Nadie cambio de posicion negativamente.");
                    return;
                }

                int enviadas = await _firebaseService.EnviarNotificacionAMultiplesUsuariosAsync(
                    usuarioSitioIds: usuariosSuperados.Distinct().ToList(),
                    tipo: TipoNotificacion.Ranking,
                    titulo: "📉 Te superaron en la tabla",
                    mensaje: "Alguien acertó y te pasó en el ranking. ¡A acertar más!",
                    data: new Dictionary<string, string>
                    {
                        { "tipo", "cambio_ranking" }
                    }
                );

                _logger.LogInformation(
                    $"[Ranking] Notificaciones de ranking enviadas: {enviadas} exitosas de {usuariosSuperados.Distinct().Count()} objetivos.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Ranking] Error al enviar notificaciones de ranking: {ex.Message}");
            }
        }

        /// <summary>
        /// Envia notificacion push a los predictores del partido informando el resultado.
        /// </summary>
        private async Task EnviarNotificacionResultadoAsync(
            Models.Partido partido,
            List<Models.Participacion> participacionesAfectadas)
        {
            try
            {
                var usuarioIds = participacionesAfectadas
                    .Select(p => p.UsuarioSitioId)
                    .Distinct()
                    .ToList();

                if (!usuarioIds.Any()) return;

                var local = partido.Local?.Nombre ?? "Local";
                var visitante = partido.Visitante?.Nombre ?? "Visitante";
                var titulo = "⚽ Resultado confirmado";
                var mensaje = $"{local} {partido.GolesLocal} - {partido.GolesVisitante} {visitante}";

                var data = new Dictionary<string, string>
                {
                    { "tipo", "resultado_partido" },
                    { "partidoId", partido.Id.ToString() }
                };

                int enviadas = await _firebaseService.EnviarNotificacionAMultiplesUsuariosAsync(
                    usuarioSitioIds: usuarioIds,
                    tipo: TipoNotificacion.Resultados,
                    titulo: titulo,
                    mensaje: mensaje,
                    data: data
                );

                _logger.LogInformation(
                    $"[ProcesadorResultados] Notificacion push enviada para partido {partido.Id}. " +
                    $"Usuarios objetivo: {usuarioIds.Count}, enviadas exitosamente: {enviadas}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ProcesadorResultados] Error al enviar notificacion push: {ex.Message}");
            }
        }

        /// <summary>
        /// Calcula los puntos de una prediccion segun los parametros configurables del sistema.
        /// 
        /// Esquema:
        ///  - Resultado exacto (ej: predijo 2-1, fue 2-1)           → PuntosResultadoExacto
        ///  - Acerto ganador + diferencia exacta (ej: predijo 2-0, fue 3-1)  → PuntosGanadorDiferenciaGoles
        ///  - Acerto solo el ganador o empate (sin diferencia exacta)        → PuntosGanadorEmpate
        ///  - No acerto nada                                                 → 0
        /// </summary>
        private int CalcularPuntos(
            int golesLocalReal,
            int golesVisitanteReal,
            int golesLocalPredichos,
            int golesVisitantePredichos,
            ParametrosSistema parametros)
        {
            // Resultado exacto: acertó exactamente los goles
            if (golesLocalReal == golesLocalPredichos && golesVisitanteReal == golesVisitantePredichos)
            {
                return parametros.PuntosResultadoExacto;
            }

            var diferenciaReal = golesLocalReal - golesVisitanteReal;
            var diferenciaPredicha = golesLocalPredichos - golesVisitantePredichos;

            var empateReal = diferenciaReal == 0;
            var empatePredicho = diferenciaPredicha == 0;

            // ¿Acertó al menos quién ganaba (o el empate)?
            var acertoGanadorOEmpate =
                (diferenciaReal > 0 && diferenciaPredicha > 0) ||
                (diferenciaReal < 0 && diferenciaPredicha < 0) ||
                (empateReal && empatePredicho);

            if (!acertoGanadorOEmpate)
            {
                return 0;
            }

            // Si acertó al ganador Y la diferencia exacta, puntaje intermedio
            if (!empateReal && diferenciaReal == diferenciaPredicha)
            {
                return parametros.PuntosGanadorDiferenciaGoles;
            }

            // Solo acertó al ganador o empate, sin diferencia exacta
            return parametros.PuntosGanadorEmpate;
        }
    }
}
