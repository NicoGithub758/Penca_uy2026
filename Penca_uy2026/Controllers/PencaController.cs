using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [Authorize]
    public class PencaController : Controller
    {
        private readonly MyDbContext _context;

        public PencaController(MyDbContext context)
        {
            _context = context;
        }

        // Listado de Pencas
        public async Task<IActionResult> Index()
        {
            var pencas = await _context.Pencas.Include(p => p.Deporte).ToListAsync();
            return View(pencas);
        }

        // GET: Formulario de Creación
        public async Task<IActionResult> Create()
        {
            var model = new PencaViewModel
            {
                Deportes = await _context.Deportes
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Nombre })
                    .ToListAsync()
            };
            return View(model);
        }

        // POST: Guardar Penca
        [HttpPost]
        public async Task<IActionResult> Create(PencaViewModel model)
        {
            if (ModelState.IsValid)
            {
                var nuevaPenca = new Penca
                {
                    Nombre = model.Nombre,
                    DeporteId = model.DeporteId,
                    CantidadEquipos = model.CantidadEquipos,
                    Modo = model.Modo
                };

                _context.Pencas.Add(nuevaPenca);

                // REEMPLAZA LA LÍNEA DEL ERROR POR ESTA:
                await _context.SaveChangesAsync();

                // Redirigimos al Index (o a la carga de equipos después)
                return RedirectToAction(nameof(Index));
            }

            // Si algo falla, recargamos la lista de deportes para el dropdown
            model.Deportes = await _context.Deportes
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Nombre })
                .ToListAsync();

            return View(model);
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
            //Borra predicciones y partidos para evitar error al eliminar todos los equipos
            
            var predicciones = _context.Predicciones.Where(p => p.Participacion.PencaInstancia.PencaId == pencaId);
            _context.Predicciones.RemoveRange(predicciones);
                
            var partidos = _context.Partidos.Where(p => p.PencaId == pencaId);
            _context.Partidos.RemoveRange(partidos);

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
                Equipos = penca.Equipos.Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.Nombre }).ToList()
            };

            return View(model);
        }

        // GET: Penca/CargarResultado/5
        public async Task<IActionResult> CargarResultado(int id)
        {
            var partido = await _context.Partidos.FindAsync(id);
            if (partido == null) return NotFound();
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

            // Si la penca es tipo Copa o FaseGruposEliminacion, aquí podrías agregar 
            // lógica para sugerir el siguiente partido del ganador.

            return RedirectToAction(nameof(Calendario), new { id = partido.PencaId });
        }

        // POST: Penca/AgendarPartido (Versión corregida para tu POST)
        [HttpPost]
        public async Task<IActionResult> AgendarPartido(PartidoViewModel model)
        {
            // Buscamos los nombres reales de los equipos en la DB según lo enviado
            if (ModelState.IsValid)
            {
                var nuevoPartido = new Partido
                {
                    PencaId = model.PencaId,
                    // Aseguramos que los nombres se guarden bien (asumiendo que model.LocalId trae el nombre)
                    idLocal = model.LocalId,
                    idVisitante = model.VisitanteId,

                    // ESTA ES LA LÍNEA CLAVE: Especificamos que es UTC
                    Fecha = DateTime.SpecifyKind(model.Fecha, DateTimeKind.Utc),

                    Fase = model.Fase,
                    Jugado = false
                };

                _context.Partidos.Add(nuevoPartido);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Calendario), new { id = model.PencaId });
            }
            return View(model);
        }
        
        // Listado de Pencas de sitio, incluyendo la participación del usuario si es que existe.
        [Authorize]
        [HttpGet("api/pencas")]
        public async Task<IActionResult> Index( [FromQuery] string slug)
        {  
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var pencas = await _context.PencaInstancias.Include(p => p.Penca)
                                    .Include(p => p.Participaciones)
                                    .Where(p => p.Sitio.Slug == slug)
                                    .Select(p=> new
                                    {
                                        p.Penca.Id,
                                        p.Penca.Nombre,
                                        p.Penca.DeporteId,
                                        p.Penca.CantidadEquipos,
                                        p.Penca.Modo,
                                        Participacion = p.Participaciones.Where(parti => parti.UsuarioSitioId == usuarioId)
                                        .Select(p => new 
                                        {
                                            p.Id,
                                            p.EstaPagado,
                                            p.PuntajeTotal,
                                            p.UsuarioSitioId,
                                            p.PencaInstanciaId,
                                        }).FirstOrDefault()
                                    }).ToListAsync();

            return Ok(pencas);
        }

    }
}