using Azure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;
using Penca_uy2026.Filters;



[Route("AdminSitioAuth")]
[AdminSitioAuthorize]
public class AdminSitioAuthController : Controller
{
    private readonly MyDbContext _context;
    private readonly AuthService _authService;

    public AdminSitioAuthController(MyDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    // URL: /AdminSitioAuth/Login
    [AllowAnonymous]
    [HttpGet("Login")]
    public IActionResult Login() => View();

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginAdminSitioViewModel model)
    {
        // 1. Validamos al usuario y obtenemos el objeto admin que contiene su SitioId
        var admin = await _authService.ValidarAdminSitioAsync(model.Email, model.Password);

        if (admin == null)
        {
            ModelState.AddModelError("", "Credenciales incorrectas.");
            return View(model);
        }

        // 2. Guardamos una cookie con el SitioId para que el sistema sepa 
        // qué datos filtrar en las siguientes peticiones.
        Response.Cookies.Append("SitioId_Admin", admin.SitioId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = true
        });

        return RedirectToAction("Index", "AdminSitioAuth");
    }
    [HttpGet("Logout")]
    public IActionResult Logout() // Ya no necesita ser async
    {
        // 1. Borramos la cookie personalizada que nosotros mismos creamos
        Response.Cookies.Delete("SitioId_Admin");

        // 2. Si también guardas el token JWT en una cookie, bórralo también
        Response.Cookies.Delete("AuthToken");

        // 3. Redirigimos al Login
        return RedirectToAction("Login", "AdminSitioAuth");
    }

    // URL: /AdminSitioAuth/Index
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        // Verificación manual de seguridad extrema
        if (!Request.Cookies.TryGetValue("SitioId_Admin", out string? sitioId))
        {
            return RedirectToAction("Login", "AdminSitioAuth");
        }

        var sitio = await _context.Sitios
            .Include(s => s.PencaInstancias)
                .ThenInclude(pi => pi.Penca)
            .FirstOrDefaultAsync(s => s.Id == int.Parse(sitioId));

        return View(sitio);
    }

    [HttpGet("CrearPencaInstancia")]
    public async Task<IActionResult> CrearPencaInstancia()
    {
        // Listamos todas las pencas globales disponibles
        var pencasDisponibles = await _context.Pencas.ToListAsync();
        ViewBag.Pencas = pencasDisponibles;
        return View(new CrearInstanciaViewModel());
    }

    [HttpPost("CrearPencaInstancia")]
    public async Task<IActionResult> CrearPencaInstancia(CrearInstanciaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Pencas = await _context.Pencas.ToListAsync();
            return View(model);
        }

        // Recuperamos el SitioId de la cookie
        if (!Request.Cookies.TryGetValue("SitioId_Admin", out string? sitioId))
            return RedirectToAction("Login");

        var nuevaInstancia = new PencaInstancia
        {
            PencaId = model.PencaId,
            SitioId = int.Parse(sitioId),
            PorcentajeComision = model.PorcentajeComision
        };

        _context.PencaInstancias.Add(nuevaInstancia);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "AdminSitioAuth");
    }

    [HttpGet("Solicitudes")]
    public async Task<IActionResult> Solicitudes()
    {
        if (!Request.Cookies.TryGetValue("SitioId_Admin", out string? sitioId))
            return RedirectToAction("Login");

        var solicitudes = await _context.SolicitudesIngreso
            .Where(s => s.SitioId == int.Parse(sitioId) && s.Estado == EstadoSolicitud.Pendiente)
            .ToListAsync();

        return View(solicitudes);
    }

    [HttpPost("AprobarSolicitud/{id}")]
    public async Task<IActionResult> AprobarSolicitud(int id)
    {
        var solicitud = await _context.SolicitudesIngreso.FindAsync(id);
        if (solicitud == null) return NotFound();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Crear el nuevo usuario basado en la solicitud
            var nuevoUsuario = new UsuarioSitio
            {
                Email = solicitud.Email,
                Nombre = solicitud.Nombre,
                PasswordHash = solicitud.PasswordHash, // Ya venía hasheada de la solicitud
                SitioId = solicitud.SitioId,
                Rol = RolUsuarioSitio.Jugador,
                Activo = true
            };

            _context.UsuariosSitio.Add(nuevoUsuario);

            // 2. Marcar solicitud como aprobada
            solicitud.Estado = EstadoSolicitud.Aprobada;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return RedirectToAction("Solicitudes");
        }
        catch
        {
            await transaction.RollbackAsync();
            return BadRequest("Error al aprobar la solicitud.");
        }
    }

    [HttpGet("EditarInstancia/{id}")]
    public async Task<IActionResult> EditarInstancia(int id)
    {
        var instancia = await _context.PencaInstancias
            .Include(pi => pi.Penca)
            .FirstOrDefaultAsync(pi => pi.Id == id);

        if (instancia == null) return NotFound();

        return View(instancia);
    }

    [HttpPost("EditarInstancia/{id}")]
    public async Task<IActionResult> EditarInstancia(int id, PencaInstancia model)
    {
        var instancia = await _context.PencaInstancias.FindAsync(id);
        if (instancia == null) return NotFound();

        instancia.PorcentajeComision = model.PorcentajeComision;

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
    [HttpGet("ConfigurarPremios/{pencaInstanciaId}")]
    public async Task<IActionResult> ConfigurarPremios(int pencaInstanciaId)
    {
        var reglas = await _context.ReglasPremios
            .Where(r => r.PencaInstanciaId == pencaInstanciaId)
            .OrderBy(r => r.Posicion)
            .ToListAsync();

        ViewBag.PencaInstanciaId = pencaInstanciaId;
        return View(reglas);
    }

    [HttpPost]
    public async Task<IActionResult> GuardarRegla(int PencaInstanciaId, int Posicion, decimal PorcentajeDelPozo)
    {
        // 1. Obtener reglas actuales
        var reglasExistentes = await _context.ReglasPremios
            .Where(r => r.PencaInstanciaId == PencaInstanciaId)
            .ToListAsync();

        // 2. Validar duplicados
        if (reglasExistentes.Any(r => r.Posicion == Posicion))
        {
            TempData["Error"] = $"La posición {Posicion} ya está asignada.";
            return RedirectToAction("ConfigurarPremios", new { pencaInstanciaId = PencaInstanciaId });
        }

        // 3. Validar suma total (incluyendo la nueva)
        var sumaActual = reglasExistentes.Sum(r => r.PorcentajeDelPozo);
        if (sumaActual + PorcentajeDelPozo > 100)
        {
            TempData["Error"] = $"La suma total de premios supera el 100%. Llevas {sumaActual}%.";
            return RedirectToAction("ConfigurarPremios", new { pencaInstanciaId = PencaInstanciaId });
        }
          
        var nuevaRegla = new ReglaPremio
        {
            PencaInstanciaId = PencaInstanciaId,
            Posicion = Posicion,
            PorcentajeDelPozo = PorcentajeDelPozo,
            SitioId = int.Parse(Request.Cookies["SitioId_Admin"] ?? "0") // Asegúrate de tener esta lógica
        };

        _context.ReglasPremios.Add(nuevaRegla);
        await _context.SaveChangesAsync();

        return RedirectToAction("ConfigurarPremios", new { pencaInstanciaId = PencaInstanciaId });
    }

    // GET: Muestra la vista para editar
    [HttpGet("EditarRegla/{id}")]
    public async Task<IActionResult> EditarRegla(int id)
    {
        var regla = await _context.ReglasPremios.FindAsync(id);
        if (regla == null) return NotFound();
        return View(regla);
    }

    [HttpPost("AdminSitioAuth/EditarRegla/{id}")]
    public async Task<IActionResult> EditarRegla(int id, ReglaPremio regla)
    {
        var reglaExistente = await _context.ReglasPremios.FindAsync(id);
        if (reglaExistente != null)
        {
            reglaExistente.Posicion = regla.Posicion;
            reglaExistente.PorcentajeDelPozo = regla.PorcentajeDelPozo;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("ConfigurarPremios", new { pencaInstanciaId = reglaExistente.PencaInstanciaId });
    }

    [HttpPost("AdminSitioAuth/BorrarRegla/{id}")]
    public async Task<IActionResult> BorrarRegla(int id)
    {
        var regla = await _context.ReglasPremios.FindAsync(id);
        if (regla != null)
        {
            int instanciaId = regla.PencaInstanciaId;
            _context.ReglasPremios.Remove(regla);
            await _context.SaveChangesAsync();
            return RedirectToAction("ConfigurarPremios", new { pencaInstanciaId = instanciaId });
        }
        return NotFound();
    }
}