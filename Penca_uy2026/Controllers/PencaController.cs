using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    [Authorize]
    public class PencaController : Controller
    {
        private readonly MyDbContext _context;
        private readonly ApiFootballService _apiFootballService;
        private readonly ProcesadorResultadosService _procesadorResultadosService;

        public PencaController(MyDbContext context, ApiFootballService apiFootballService, ProcesadorResultadosService procesadorResultadosService)
        {
            _context = context;
            _apiFootballService = apiFootballService;
            _procesadorResultadosService = procesadorResultadosService;
        }

        // Listado de Pencas
        public async Task<IActionResult> Index()
        {
            var pencas = await _context.Pencas
                .Include(p => p.Deporte)
                .ToListAsync();

            return View(pencas);
        }

        // Selección de competición para crear una Penca
        public async Task<IActionResult> Create()
        {
            var ligas = await _apiFootballService.ObtenerLigasActualesAsync();
            return View(ligas);
        }

        // GET: Formulario de Creación
        /*public async Task<IActionResult> Create()
        {
            var model = new PencaViewModel
            {
                Deportes = await _context.Deportes
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Nombre })
                    .ToListAsync()
            };
            return View(model);
        }*/

        private async Task RecargarDeportesAsync(PencaViewModel model)
        {
            model.Deportes = await _context.Deportes
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Nombre })
                .ToListAsync();
        }

        // POST: Guardar Penca

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearDesdeCompeticion(
            int leagueId,
            int season,
            string nombreLiga,
            string pais,
            string? logoUrl)
        {
            var equiposApi = await _apiFootballService.GetTeamsAsync(leagueId, season);

            var nuevaPenca = new Penca
            {
                Nombre = $"{nombreLiga} {season}",
                DeporteId = 1,
                CantidadEquipos = equiposApi.Count,
                Modo = ModoPenca.Liga,
                ApiFootballLeagueId = leagueId,
                ApiFootballSeason = season,
                ApiFootballLeagueName = nombreLiga,
                ApiFootballCountry = pais
            };

            _context.Pencas.Add(nuevaPenca);
            await _context.SaveChangesAsync();

            foreach (var equipoApi in equiposApi)
            {
                _context.Equipos.Add(new Equipo
                {
                    PencaId = nuevaPenca.Id,
                    Nombre = equipoApi.Name,
                    ApiFootballTeamId = equipoApi.Id,
                    Codigo = equipoApi.Code,
                    Pais = equipoApi.Country,
                    LogoUrl = equipoApi.Logo
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Calendario), new { id = nuevaPenca.Id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PencaViewModel model)
        {
            if (!model.ApiFootballLeagueId.HasValue)
            {
                ModelState.AddModelError(nameof(model.ApiFootballLeagueId), "Debes ingresar el ID de la competición.");
            }

            if (!model.ApiFootballSeason.HasValue)
            {
                ModelState.AddModelError(nameof(model.ApiFootballSeason), "Debes ingresar la temporada.");
            }

            if (!ModelState.IsValid)
            {
                await RecargarDeportesAsync(model);
                return View(model);
            }

            var equiposApi = await _apiFootballService.GetTeamsAsync(
                model.ApiFootballLeagueId.Value,
                model.ApiFootballSeason.Value);

            if (!equiposApi.Any())
            {
                ModelState.AddModelError("", "No se encontraron equipos para esa competición y temporada.");
                await RecargarDeportesAsync(model);
                return View(model);
            }

            var nuevaPenca = new Penca
            {
                Nombre = model.Nombre,
                DeporteId = model.DeporteId,
                CantidadEquipos = equiposApi.Count,
                Modo = model.Modo,
                ApiFootballLeagueId = model.ApiFootballLeagueId.Value,
                ApiFootballSeason = model.ApiFootballSeason.Value
            };

            foreach (var equipoApi in equiposApi)
            {
                nuevaPenca.Equipos.Add(new Equipo
                {
                    Nombre = equipoApi.Name,
                    ApiFootballTeamId = equipoApi.Id,
                    LogoUrl = equipoApi.Logo
                });
            }

            _context.Pencas.Add(nuevaPenca);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Penca/ConfigurarEquipos/5
        public async Task<IActionResult> ConfigurarEquipos(int id)
        {
            var penca = await _context.Pencas.FindAsync(id);
            if (penca == null) return NotFound();

            ViewBag.PencaNombre = penca.Nombre;
            ViewBag.PencaId = penca.Id;

            // Pasamos la cantidad para generar los inputs en la vista
            return View(penca.CantidadEquipos);
        }

        // POST: Penca/GuardarEquipos
        [HttpPost]
        public async Task<IActionResult> GuardarEquipos(int pencaId, List<string> nombresEquipos)
        {
            // Borramos equipos previos si existen (para poder editar)
            var equiposAntiguos = _context.Equipos.Where(e => e.PencaId == pencaId);
            _context.Equipos.RemoveRange(equiposAntiguos);

            foreach (var nombre in nombresEquipos)
            {
                if (!string.IsNullOrWhiteSpace(nombre))
                {
                    _context.Equipos.Add(new Equipo
                    {
                        Nombre = nombre,
                        PencaId = pencaId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Penca/Calendario/5
        public async Task<IActionResult> Calendario(int id)
        {
            var penca = await _context.Pencas
                .Include(p => p.Partidos)
                .Include(p => p.Equipos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (penca == null) return NotFound();

            return View(penca);
        }

        // GET: Penca/AgendarPartido/5
        public async Task<IActionResult> AgendarPartido(int id)
        {
            var penca = await _context.Pencas.Include(p => p.Equipos).FirstOrDefaultAsync(p => p.Id == id);
            if (penca == null) return NotFound();

            var model = new PartidoViewModel
            {
                PencaId = id,
                PencaNombre = penca.Nombre,
                Equipos = penca.Equipos
                .OrderBy(e => e.Nombre)
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.Nombre }).ToList()
            };

            return View(model);
        }

        // GET: Penca/CargarResultado/5
        public async Task<IActionResult> CargarResultado(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null) return NotFound();

            var equipos = await _context.Equipos
                .Where(e => e.Id == partido.LocalEquipoId || e.Id == partido.VisitanteEquipoId)
                .ToListAsync();

            ViewBag.LocalNombre = equipos.FirstOrDefault(e => e.Id == partido.LocalEquipoId)?.Nombre ?? "Equipo no encontrado";
            ViewBag.VisitanteNombre = equipos.FirstOrDefault(e => e.Id == partido.VisitanteEquipoId)?.Nombre ?? "Equipo no encontrado";

            return View(partido);
        }

        // POST: Penca/CargarResultado
        [HttpPost]
        public async Task<IActionResult> CargarResultado(int id, int golesLocal, int golesVisitante)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null) return NotFound();

            partido.GolesLocal = golesLocal;
            partido.GolesVisitante = golesVisitante;
            partido.Jugado = true;

            await _context.SaveChangesAsync();

            // Lógica para procesar los puntos y notificar por SignalR (Carga o Edición)
            await _procesadorResultadosService.ProcesarPartidoAsync(partido.Id);

            // Si la penca es tipo Copa o FaseGruposEliminacion, aquí podrías agregar 
            // lógica para sugerir el siguiente partido del ganador.

            return RedirectToAction(nameof(Calendario), new { id = partido.PencaId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarResultadoDesdeApi(int id)
        {
            var partido = await _context.Partidos.FirstOrDefaultAsync(p => p.Id == id);
            if (partido == null) return NotFound();

            var penca = await _context.Pencas
                .Include(p => p.Equipos)
                .FirstOrDefaultAsync(p => p.Id == partido.PencaId);

            if (penca == null) return NotFound();

            if (!penca.ApiFootballLeagueId.HasValue || !penca.ApiFootballSeason.HasValue)
            {
                TempData["Error"] = "La penca no tiene datos de API-Football configurados.";
                return RedirectToAction(nameof(Calendario), new { id = partido.PencaId });
            }

            var equipoLocal = penca.Equipos.FirstOrDefault(e => e.Id == partido.LocalEquipoId);
            var equipoVisitante = penca.Equipos.FirstOrDefault(e => e.Id == partido.VisitanteEquipoId);

            if (equipoLocal?.ApiFootballTeamId == null || equipoVisitante?.ApiFootballTeamId == null)
            {
                TempData["Error"] = "No se encontraron los IDs de API-Football para los equipos.";
                return RedirectToAction(nameof(Calendario), new { id = partido.PencaId });
            }

            var fixture = await _apiFootballService.GetFixtureResultAsync(
                penca.ApiFootballLeagueId.Value,
                penca.ApiFootballSeason.Value,
                equipoLocal.ApiFootballTeamId.Value,
                equipoVisitante.ApiFootballTeamId.Value,
                partido.Fecha);

            if (fixture == null)
            {
                TempData["Error"] = "No se encontró el partido en API-Football.";
                return RedirectToAction(nameof(Calendario), new { id = partido.PencaId });
            }

            var estadosFinalizados = new[] { "FT", "AET", "PEN" };

            if (!estadosFinalizados.Contains(fixture.Fixture.Status.Short))
            {
                TempData["Error"] = "El partido todavía no está finalizado.";
                return RedirectToAction(nameof(Calendario), new { id = partido.PencaId });
            }

            if (!fixture.Goals.Home.HasValue || !fixture.Goals.Away.HasValue)
            {
                TempData["Error"] = "API-Football no devolvió goles para este partido.";
                return RedirectToAction(nameof(Calendario), new { id = partido.PencaId });
            }

            partido.GolesLocal = fixture.Goals.Home.Value;
            partido.GolesVisitante = fixture.Goals.Away.Value;
            partido.ApiFootballFixtureId = fixture.Fixture.Id;
            partido.Jugado = true;

            await _context.SaveChangesAsync();

            // Lógica para procesar los puntos y notificar por SignalR (Llamado manual a API)
            await _procesadorResultadosService.ProcesarPartidoAsync(partido.Id);

            TempData["Success"] = "Resultado actualizado desde API-Football.";
            return RedirectToAction(nameof(Calendario), new { id = partido.PencaId });
        }

        // POST: Penca/AgendarPartido (Versión corregida para tu POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgendarPartido(PartidoViewModel model)
        {
            if (model.LocalId == model.VisitanteId)
            {
                ModelState.AddModelError("", "El equipo local y visitante no pueden ser el mismo.");
            }

            if (!ModelState.IsValid)
            {
                var penca = await _context.Pencas
                    .Include(p => p.Equipos)
                    .FirstOrDefaultAsync(p => p.Id == model.PencaId);

                if (penca == null) return NotFound();

                model.PencaNombre = penca.Nombre;
                model.Equipos = penca.Equipos
                    .OrderBy(e => e.Nombre)
                    .Select(e => new SelectListItem
                    {
                        Value = e.Id.ToString(),
                        Text = e.Nombre
                    })
                    .ToList();

                return View(model);
            }

            var nuevoPartido = new Partido
            {
                PencaId = model.PencaId,
                LocalEquipoId = model.LocalId,
                VisitanteEquipoId = model.VisitanteId,
                Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc),
                Fase = model.Fase,
                Jugado = false
            };

            _context.Partidos.Add(nuevoPartido);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Calendario), new { id = model.PencaId });
        }

        
    }
}
