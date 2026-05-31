using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;

namespace Penca_uy2026.Services
{
    public class ActualizarResultadosService
    {
        private readonly MyDbContext _context;
        private readonly ApiFootballService _apiFootballService;
        private readonly ParametrosSistemaService _parametrosSistemaService;
        private readonly ILogger<ActualizarResultadosService> _logger;

        public ActualizarResultadosService(
            MyDbContext context,
            ApiFootballService apiFootballService,
            ParametrosSistemaService parametrosSistemaService,
            ILogger<ActualizarResultadosService> logger)
        {
            _context = context;
            _apiFootballService = apiFootballService;
            _parametrosSistemaService = parametrosSistemaService;
            _logger = logger;
        }

        public async Task ActualizarResultadosAsync(CancellationToken cancellationToken = default)
        {
            var parametrosSistema = await _parametrosSistemaService.ObtenerAsync(cancellationToken);

            if (!parametrosSistema.ActualizacionAutomaticaResultadosActiva)
            {
                _logger.LogInformation("Actualizacion automatica de resultados desactivada por parametros del sistema.");
                return;
            }

            var zonaHorariaSistema = await _parametrosSistemaService.ObtenerTimeZoneInfoAsync(cancellationToken);
            var ahoraSistema = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaHorariaSistema);
            var ahoraUtc = DateTime.UtcNow;
            var limiteFinPartido = ahoraSistema.AddMinutes(-parametrosSistema.MinutosDespuesInicioParaConsultarResultado);
            var limiteUltimaConsulta = ahoraUtc.AddMinutes(-parametrosSistema.IntervaloMinutosConsultaResultados);

            _logger.LogInformation(
                "Buscando partidos pendientes para actualizar. TimeZone={TimeZone}, AhoraSistema={AhoraSistema}, LimiteFinPartido={LimiteFinPartido}, AhoraUtc={AhoraUtc}, LimiteUltimaConsultaUtc={LimiteUltimaConsultaUtc}",
                parametrosSistema.TimeZoneId,
                ahoraSistema,
                limiteFinPartido,
                ahoraUtc,
                limiteUltimaConsulta);

            var partidos = await _context.Partidos
                .Where(p =>
                    !p.Jugado &&
                    p.Fecha <= limiteFinPartido &&
                    (p.UltimaConsultaApi == null || p.UltimaConsultaApi <= limiteUltimaConsulta))
                .ToListAsync(cancellationToken);

            if (!partidos.Any())
            {
                _logger.LogInformation("No hay partidos candidatos para actualizar.");
                return;
            }

            _logger.LogInformation("Partidos candidatos para actualizar: {Count}", partidos.Count);

            var pencaIds = partidos.Select(p => p.PencaId).Distinct().ToList();

            var pencas = await _context.Pencas
                .Where(p => pencaIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var equipoIds = partidos
                .SelectMany(p => new[] { p.LocalEquipoId, p.VisitanteEquipoId })
                .Distinct()
                .ToList();

            var equipos = await _context.Equipos
                .Where(e => equipoIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, cancellationToken);

            foreach (var partido in partidos)
            {
                var tienePenca = pencas.TryGetValue(partido.PencaId, out var penca);
                var tieneLocal = equipos.TryGetValue(partido.LocalEquipoId, out var local);
                var tieneVisitante = equipos.TryGetValue(partido.VisitanteEquipoId, out var visitante);

                if (!tienePenca || !tieneLocal || !tieneVisitante)
                {
                    _logger.LogWarning(
                        "Partido candidato descartado por relaciones faltantes. PartidoId={PartidoId}, PencaId={PencaId}, TienePenca={TienePenca}, LocalEquipoId={LocalEquipoId}, TieneLocal={TieneLocal}, VisitanteEquipoId={VisitanteEquipoId}, TieneVisitante={TieneVisitante}",
                        partido.Id,
                        partido.PencaId,
                        tienePenca,
                        partido.LocalEquipoId,
                        tieneLocal,
                        partido.VisitanteEquipoId,
                        tieneVisitante);
                    continue;
                }

                if (!penca!.ApiFootballLeagueId.HasValue ||
                    !penca.ApiFootballSeason.HasValue ||
                    !local!.ApiFootballTeamId.HasValue ||
                    !visitante!.ApiFootballTeamId.HasValue)
                {
                    _logger.LogWarning(
                        "Partido candidato descartado por datos API-Football incompletos. PartidoId={PartidoId}, LeagueId={LeagueId}, Season={Season}, LocalEquipoId={LocalEquipoId}, LocalNombre={LocalNombre}, LocalApiId={LocalApiId}, VisitanteEquipoId={VisitanteEquipoId}, VisitanteNombre={VisitanteNombre}, VisitanteApiId={VisitanteApiId}",
                        partido.Id,
                        penca.ApiFootballLeagueId,
                        penca.ApiFootballSeason,
                        local.Id,
                        local.Nombre,
                        local.ApiFootballTeamId,
                        visitante.Id,
                        visitante.Nombre,
                        visitante.ApiFootballTeamId);
                }
            }

            var partidosValidos = partidos
                .Where(p =>
                    pencas.ContainsKey(p.PencaId) &&
                    equipos.ContainsKey(p.LocalEquipoId) &&
                    equipos.ContainsKey(p.VisitanteEquipoId))
                .Select(p => new
                {
                    Partido = p,
                    Penca = pencas[p.PencaId],
                    Local = equipos[p.LocalEquipoId],
                    Visitante = equipos[p.VisitanteEquipoId],
                    Fecha = p.Fecha.Date
                })
                .Where(x =>
                    x.Penca.ApiFootballLeagueId.HasValue &&
                    x.Penca.ApiFootballSeason.HasValue &&
                    x.Local.ApiFootballTeamId.HasValue &&
                    x.Visitante.ApiFootballTeamId.HasValue)
                .ToList();

            _logger.LogInformation("Partidos validos con datos API-Football completos: {Count}", partidosValidos.Count);

            var grupos = partidosValidos
                .GroupBy(x => new
                {
                    LeagueId = x.Penca.ApiFootballLeagueId!.Value,
                    Season = x.Penca.ApiFootballSeason!.Value,
                    Fecha = x.Fecha
                });

            foreach (var grupo in grupos)
            {
                _logger.LogInformation(
                    "Consultando fixtures por liga y fecha. LeagueId={LeagueId}, Season={Season}, Fecha={Fecha}, PartidosGrupo={PartidosGrupo}",
                    grupo.Key.LeagueId,
                    grupo.Key.Season,
                    grupo.Key.Fecha.ToString("yyyy-MM-dd"),
                    grupo.Count());

                var fixtures = await _apiFootballService.GetFixturesByLeagueDateAsync(
                    grupo.Key.LeagueId,
                    grupo.Key.Season,
                    grupo.Key.Fecha);

                foreach (var item in grupo)
                {
                    item.Partido.UltimaConsultaApi = ahoraUtc;
                    item.Partido.IntentosConsultaApi++;

                    var localApiId = item.Local.ApiFootballTeamId!.Value;
                    var visitanteApiId = item.Visitante.ApiFootballTeamId!.Value;

                    var fixture = BuscarFixture(fixtures, localApiId, visitanteApiId);

                    if (fixture == null)
                    {
                        _logger.LogWarning(
                            "No se encontro fixture para PartidoId={PartidoId}. LocalApiId={LocalApiId}, VisitanteApiId={VisitanteApiId}, Fecha={Fecha}",
                            item.Partido.Id,
                            localApiId,
                            visitanteApiId,
                            grupo.Key.Fecha.ToString("yyyy-MM-dd"));
                        continue;
                    }

                    item.Partido.ApiFootballStatus = fixture.Fixture.Status.Short;
                    item.Partido.ApiFootballFixtureId = fixture.Fixture.Id;

                    if (fixture.Fixture.Status.Short is not ("FT" or "AET" or "PEN"))
                    {
                        _logger.LogInformation(
                            "Fixture encontrado pero no finalizado. PartidoId={PartidoId}, FixtureId={FixtureId}, Status={Status}",
                            item.Partido.Id,
                            fixture.Fixture.Id,
                            fixture.Fixture.Status.Short);
                        continue;
                    }

                    GuardarResultadoFulltime(item.Partido, fixture, localApiId, visitanteApiId);

                    if (item.Partido.Jugado)
                    {
                        _logger.LogInformation(
                            "Resultado actualizado desde API-Football. PartidoId={PartidoId}, FixtureId={FixtureId}, Status={Status}, Resultado={GolesLocal}-{GolesVisitante}",
                            item.Partido.Id,
                            fixture.Fixture.Id,
                            fixture.Fixture.Status.Short,
                            item.Partido.GolesLocal,
                            item.Partido.GolesVisitante);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Fixture finalizado sin score.fulltime completo. PartidoId={PartidoId}, FixtureId={FixtureId}, Status={Status}",
                            item.Partido.Id,
                            fixture.Fixture.Id,
                            fixture.Fixture.Status.Short);
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static ApiFootballFixtureItem? BuscarFixture(
            List<ApiFootballFixtureItem> fixtures,
            int localApiId,
            int visitanteApiId)
        {
            return fixtures.FirstOrDefault(f =>
                (f.Teams.Home.Id == localApiId && f.Teams.Away.Id == visitanteApiId) ||
                (f.Teams.Home.Id == visitanteApiId && f.Teams.Away.Id == localApiId));
        }

        private static void GuardarResultadoFulltime(
            Partido partido,
            ApiFootballFixtureItem fixture,
            int localApiId,
            int visitanteApiId)
        {
            var fixtureLocalEsLocalDelPartido = fixture.Teams.Home.Id == localApiId;

            var golesLocal = fixtureLocalEsLocalDelPartido
                ? fixture.Score.Fulltime.Home
                : fixture.Score.Fulltime.Away;

            var golesVisitante = fixtureLocalEsLocalDelPartido
                ? fixture.Score.Fulltime.Away
                : fixture.Score.Fulltime.Home;

            if (!golesLocal.HasValue || !golesVisitante.HasValue)
                return;

            partido.GolesLocal = golesLocal.Value;
            partido.GolesVisitante = golesVisitante.Value;
            partido.Jugado = true;
        }

    }
}
