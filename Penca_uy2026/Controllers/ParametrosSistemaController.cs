using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Penca_uy2026.Models;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    [Authorize(Roles = "PlataformaAdmin")]
    public class ParametrosSistemaController : Controller
    {
        private readonly ParametrosSistemaService _parametrosSistemaService;

        public ParametrosSistemaController(ParametrosSistemaService parametrosSistemaService)
        {
            _parametrosSistemaService = parametrosSistemaService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var parametros = await _parametrosSistemaService.ObtenerAsync();
            CargarTimeZones(parametros.TimeZoneId);

            return View(parametros);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ParametrosSistema model)
        {
            if (!ParametrosSistemaService.EsTimeZoneValido(model.TimeZoneId))
            {
                ModelState.AddModelError(nameof(model.TimeZoneId), "El huso horario seleccionado no es valido.");
            }

            foreach (var error in ParametrosSistemaService.ValidarPuntajes(model))
            {
                ModelState.AddModelError(error.Campo, error.Mensaje);
            }

            foreach (var error in ParametrosSistemaService.ValidarComision(model))
            {
                ModelState.AddModelError(error.Campo, error.Mensaje);
            }

            if (!ModelState.IsValid)
            {
                CargarTimeZones(model.TimeZoneId);
                return View(model);
            }

            await _parametrosSistemaService.ActualizarAsync(model);

            TempData["Success"] = "Parametros del sistema actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private void CargarTimeZones(string? seleccionado)
        {
            var timeZones = new List<SelectListItem>
            {
                new SelectListItem("Uruguay - Montevideo", "America/Montevideo"),
                new SelectListItem("Argentina - Buenos Aires", "America/Argentina/Buenos_Aires"),
                new SelectListItem("Brasil - Sao Paulo", "America/Sao_Paulo"),
                new SelectListItem("Chile - Santiago", "America/Santiago"),
                new SelectListItem("Paraguay - Asuncion", "America/Asuncion"),
                new SelectListItem("Colombia - Bogota", "America/Bogota"),
                new SelectListItem("Peru - Lima", "America/Lima"),
                new SelectListItem("Mexico - Ciudad de Mexico", "America/Mexico_City"),
                new SelectListItem("Estados Unidos - New York", "America/New_York"),
                new SelectListItem("Espana - Madrid", "Europe/Madrid"),
                new SelectListItem("UTC", "UTC")
            };

            foreach (var item in timeZones)
            {
                item.Selected = item.Value == seleccionado;
            }

            ViewBag.TimeZones = timeZones;
        }
    }
}
