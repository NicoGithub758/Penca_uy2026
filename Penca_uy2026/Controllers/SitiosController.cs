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

        public SitiosController(MyDbContext context, SitioService sitioService)
        {
            _context = context;
            _sitioService = sitioService;
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

        /// <summary>
        /// Obtiene la lista de sitios públicos y activos para la Landing Page.
        /// </summary>
        [HttpGet("api/sitios/publicos")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSitiosPublicos()
        {
            var sitios = await _context.Sitios
                .Where(s => s.Activo && (s.TipoRegistro == TipoRegistro.Abierta || s.TipoRegistro == TipoRegistro.AbiertaConAutorizacion))
                .Select(s => new { 
                    name = s.Nombre, 
                    slug = s.Slug, 
                    active = s.Activo,
                    tipoRegistro = s.TipoRegistro,
                    logoUrl = s.LogoUrl,
                    colorPrincipal = s.ColorPrincipal
                })
                .ToListAsync();

            return Ok(sitios);
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
            
            var nuevaPencaInstancia = new PencaInstancia
            {
                Costo = costo,
                PorcentajeComision = 5,
                PencaId = pencaId, 
                SitioId = sitioId
            };

            _context.Add(nuevaPencaInstancia);
            _context.SaveChanges();

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
            .Where(p => !_context.PencaInstancias
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
