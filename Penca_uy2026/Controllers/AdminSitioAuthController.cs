using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Data;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;
using Microsoft.EntityFrameworkCore;

[AllowAnonymous]
[Route("AdminSitioAuth")]
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
    [HttpGet("Login")]
    public IActionResult Login() => View();

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

    // URL: /AdminSitioAuth/Index
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        // Recuperamos el SitioId desde la cookie
        if (!Request.Cookies.TryGetValue("SitioId_Admin", out string? sitioId))
            return RedirectToAction("Login");

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
}