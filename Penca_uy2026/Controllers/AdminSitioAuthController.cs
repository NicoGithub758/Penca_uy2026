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
    private readonly ImageService _imageService;
    private readonly IConfiguration _configuration;
    private readonly ParametrosSistemaService _parametrosSistemaService;

    public AdminSitioAuthController(MyDbContext context, AuthService authService, ImageService imageService, IConfiguration configuration, ParametrosSistemaService parametrosSistemaService)
    {
        _context = context;
        _authService = authService;
        _imageService = imageService;
        _configuration = configuration;
        _parametrosSistemaService = parametrosSistemaService;
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
        // Listamos solo las pencas globales disponibles para ser agregadas a sitios.
        var pencasDisponibles = await _context.Pencas
            .Where(p => !p.Finalizada)
            .ToListAsync();

        ViewBag.Pencas = pencasDisponibles;
        return View(new CrearInstanciaViewModel());
    }

    [HttpPost("CrearPencaInstancia")]
    public async Task<IActionResult> CrearPencaInstancia(CrearInstanciaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Pencas = await _context.Pencas
                .Where(p => !p.Finalizada)
                .ToListAsync();

            return View(model);
        }

        // Recuperamos el SitioId de la cookie
        if (!Request.Cookies.TryGetValue("SitioId_Admin", out string? sitioId))
            return RedirectToAction("Login");

        var penca = await _context.Pencas.FindAsync(model.PencaId);

        if (penca == null || penca.Finalizada)
        {
            ModelState.AddModelError("", "No se puede agregar una penca finalizada a un sitio.");

            ViewBag.Pencas = await _context.Pencas
                .Where(p => !p.Finalizada)
                .ToListAsync();

            return View(model);
        }

        var parametros = await _parametrosSistemaService.ObtenerAsync();

        var nuevaInstancia = new PencaInstancia
        {
            PencaId = model.PencaId,
            SitioId = int.Parse(sitioId),
            PorcentajeComision = parametros.PorcentajeComisionPenca,
            Costo = model.Costo
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

        instancia.Costo = model.Costo;

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

    [HttpGet("Configuracion")]
    public async Task<IActionResult> Configuracion()
    {
        if (!Request.Cookies.TryGetValue("SitioId_Admin", out string? sitioId))
            return RedirectToAction("Login", "AdminSitioAuth");

        var sitio = await _context.Sitios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == int.Parse(sitioId));

        if (sitio == null) return NotFound();

        return View(sitio);
    }

    [HttpPost("Configuracion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configuracion(Sitio modelo, IFormFile? logoFile)
    {
        if (!Request.Cookies.TryGetValue("SitioId_Admin", out string? sitioIdStr) || !int.TryParse(sitioIdStr, out int sitioId))
            return RedirectToAction("Login", "AdminSitioAuth");

        var sitioOriginal = await _context.Sitios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == sitioId);

        if (sitioOriginal == null) return NotFound();

        // Limpiamos los errores del modelo que no nos importan (ej: campos que el admin no toca)
        ModelState.Remove("Url");
        
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // Subir nuevo logo si se proporciona uno
        if (logoFile != null && logoFile.Length > 0)
        {
            using var stream = logoFile.OpenReadStream();
            var newLogoUrl = await _imageService.UploadImageAsync(stream, logoFile.FileName, cropToSquare: false);
            if (!string.IsNullOrEmpty(newLogoUrl))
            {
                sitioOriginal.LogoUrl = newLogoUrl;
            }
        }

        sitioOriginal.Nombre = modelo.Nombre;
        sitioOriginal.Descripcion = modelo.Descripcion;
        sitioOriginal.ColorPrincipal = modelo.ColorPrincipal;
        sitioOriginal.TipoRegistro = modelo.TipoRegistro;

        // Recalcular URL basada en el Slug si este cambió
        if (!string.IsNullOrWhiteSpace(modelo.Slug) && modelo.Slug.Trim().ToLower() != sitioOriginal.Slug)
        {
            sitioOriginal.Slug = modelo.Slug.Trim().ToLower();
            var frontendDomain = _configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173";
            sitioOriginal.Url = $"{frontendDomain}/{sitioOriginal.Slug}";
        }

        _context.Sitios.Update(sitioOriginal);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Configuración del sitio actualizada correctamente.";
        return RedirectToAction("Configuracion");
    }
    [HttpGet("Estadisticas")]
    public async Task<IActionResult> Estadisticas()
    {
        if (!Request.Cookies.TryGetValue("SitioId_Admin", out string? sitioIdStr) || !int.TryParse(sitioIdStr, out int sitioId))
            return RedirectToAction("Login", "AdminSitioAuth");

        var model = new EstadisticasAdminSitioViewModel
        {
            // ── KPIs ─────────────────────────────────────────────────────

            TotalUsuarios = await _context.UsuariosSitio
                .CountAsync(u => u.SitioId == sitioId
                              && u.Rol == RolUsuarioSitio.Jugador),

            DineroRecaudado = await _context.Pagos
                .Where(p => p.SitioId == sitioId && p.Estado == "COMPLETED")
                .SumAsync(p => (decimal?)p.Monto) ?? 0m,

            TotalPencasActivas = await _context.PencaInstancias
                .CountAsync(pi => pi.SitioId == sitioId),

            // ── Gráfico 1 — Ingresos mensuales ───────────────────────────

            IngresosMensuales = await _context.Pagos
                .Where(p => p.SitioId == sitioId && p.Estado == "COMPLETED")
                .GroupBy(p => new { p.FechaPago.Year, p.FechaPago.Month })
                .Select(g => new IngresoMensualSitioDTO
                {
                    Anio = g.Key.Year,
                    Mes = g.Key.Month,
                    Monto = g.Sum(x => x.Monto)
                })
                .OrderBy(x => x.Anio).ThenBy(x => x.Mes)
                .ToListAsync(),

            // ── Gráfico 2 — Pencas con más jugadores ─────────────────────
            // Nombre viene de Penca.Nombre a través de PencaInstancia.Penca

            PencasPorJugadores = await _context.Participaciones
                .Where(p => p.PencaInstancia.SitioId == sitioId)
                .GroupBy(p => p.PencaInstancia.Penca.Nombre)
                .Select(g => new PencaJugadoresDTO
                {
                    NombrePenca = g.Key,
                    CantidadJugadores = g.Count()
                })
                .OrderByDescending(x => x.CantidadJugadores)
                .Take(10)
                .ToListAsync(),

            // ── Gráfico 3 — Pencas que más recaudaron ────────────────────
            // MontoRecaudado = cantidad de participantes × Costo de la PencaInstancia

            PencasPorRecaudacion = await _context.Participaciones
                .Where(p => p.PencaInstancia.SitioId == sitioId)
                .GroupBy(p => new
                {
                    Nombre = p.PencaInstancia.Penca.Nombre,
                    Costo = p.PencaInstancia.Costo
                })
                .Select(g => new PencaRecaudacionDTO
                {
                    NombrePenca = g.Key.Nombre,
                    MontoRecaudado = g.Count() * g.Key.Costo
                })
                .OrderByDescending(x => x.MontoRecaudado)
                .Take(10)
                .ToListAsync(),

            // ── Gráfico 4 — Usuarios con / sin cuenta mobile ─────────────
            // Se considera "con mobile" si tiene FcmToken registrado

            UsuariosConMobile = await _context.UsuariosSitio
                .CountAsync(u => u.SitioId == sitioId
                              && u.Rol == RolUsuarioSitio.Jugador
                              && u.FcmToken != null),

            UsuariosSinMobile = await _context.UsuariosSitio
                .CountAsync(u => u.SitioId == sitioId
                              && u.Rol == RolUsuarioSitio.Jugador
                              && u.FcmToken == null),

            // ── Gráfico 5 — Pencas con más predicciones ──────────────────
            // Prediccion → Participacion → PencaInstancia → Penca.Nombre

            PencasPorPredicciones = await _context.Predicciones
                .Where(p => p.Participacion.PencaInstancia.SitioId == sitioId)
                .GroupBy(p => p.Participacion.PencaInstancia.Penca.Nombre)
                .Select(g => new PencaPrediccionesDTO
                {
                    NombrePenca = g.Key,
                    CantidadPredicciones = g.Count()
                })
                .OrderByDescending(x => x.CantidadPredicciones)
                .Take(10)
                .ToListAsync(),
        };

        return View(model);
    }


}
