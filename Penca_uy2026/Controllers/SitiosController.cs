using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Data;
using Penca_uy2026.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.AppConfig;
using System.Security.Claims;
using Penca_uy2026.Models;


namespace Penca_uy2026.Controllers
{
    /// <summary>
    /// Controlador para la gestión de sitios. 
    /// Combina acciones de Backoffice (Views) con endpoints de API para el frontend.
    /// </summary>
//  [Authorize(Roles = "PlataformaAdmin")]
    public class SitiosController : Controller
    {
        private readonly MyDbContext _context;
        private readonly SitioService _sitioService;
        private readonly ParametrosSistemaService _parametrosSistemaService;

        public SitiosController(
            MyDbContext context,
            SitioService sitioService,
            ParametrosSistemaService parametrosSistemaService)
        {
            _context = context;
            _sitioService = sitioService;
            _parametrosSistemaService = parametrosSistemaService;
        }

        // --- ACCIONES DE BACKOFFICE (Razor Views) ---

        public IActionResult Index()
        {
            var sitios = _context.Sitios.ToList();
            return View(sitios);
        }

        // --- ENDPOINTS DE API (Para el Frontend React) ---

        /// <summary>
        /// Verifica si un slug pertenece a un sitio registrado y activo.
        /// Este endpoint es público porque se usa antes de que el usuario se loguee.
        /// </summary>
        [HttpGet("api/sitios/validar/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarSlug(string slug)
        {
            // Delegamos la lógica de negocio al servicio (Arquitectura N-Tier)
            var sitio = await _sitioService.ObtenerConfiguracionPorSlugAsync(slug);

            if (sitio == null)
            {
                return NotFound(new { mensaje = "El sitio especificado no existe o no está disponible." });
            }

            return Ok(sitio);
        }
        

        //Asocia una penca creada en el backoffice a un sitio, creando nueva penca instancia.
        [Authorize]
        [HttpPost("api/sitios/asociarpenca")]
        public async Task<IActionResult> AsociarPenca([FromQuery] int costo, [FromQuery] int pencaId){
            
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var sitioId = int.Parse(User.FindFirstValue("sitioId"));
            var usuarioRol = User.FindFirstValue(ClaimTypes.Role);

            if(usuarioRol != "AdminSitio")
                return BadRequest("Usuario no es administrador.");

            var penca = await _context.Pencas.FindAsync(pencaId);

            if (penca == null)
                return NotFound("La penca no existe.");

            if (penca.Finalizada)
                return BadRequest("No se puede agregar una penca finalizada a un sitio.");

            var parametros = await _parametrosSistemaService.ObtenerAsync();
            
            var nuevaPencaInstancia = new PencaInstancia
            {
                Costo = costo,
                PorcentajeComision = parametros.PorcentajeComisionPenca,
                PencaId = pencaId, 
                SitioId = sitioId
            };

            _context.Add(nuevaPencaInstancia);
            await _context.SaveChangesAsync();

            return Ok();
        }

    // Trae todas las pencas del sistema para que el administrador pueda asociarlas al sitio.
    [Authorize]
    [HttpGet("api/pencasSistema")]
    public async Task<IActionResult> GetPencasSistema(){
           
        var sitioId = int.Parse(User.FindFirstValue("sitioId"));
        var usuarioRol = User.FindFirstValue(ClaimTypes.Role);

        Console.WriteLine($"ROL: {usuarioRol}");
        
        /*if(usuarioRol != 1)
            return BadRequest("Usuario no es administrador.");*/

        var pencas = await _context.Pencas
            .Where(p => !p.Finalizada && !_context.PencaInstancias
                .Any(pi => pi.PencaId == p.Id && pi.SitioId == sitioId))
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.CantidadEquipos,
                p.Modo,
                p.DeporteId
            }).ToListAsync();
        return Ok(pencas);
        }

        
    }
}
