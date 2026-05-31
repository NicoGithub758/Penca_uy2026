using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Asegúrate de tener este using
using Penca_uy2026.Data;
using Penca_uy2026.Models; // Necesario para Sitio y UsuarioSitio
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;

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

    [HttpGet("Login/{slug}")]
    public IActionResult Login(string slug)
    {
        ViewBag.Slug = slug;
        return View();
    }

    [HttpPost("Login/{slug}")]
    public async Task<IActionResult> Login(string slug, LoginAdminSitioViewModel model)
    {
        // Solución: Especificamos <Sitio>
        var sitio = await _context.Sitios.FirstOrDefaultAsync<Sitio>(s => s.Slug == slug);
        if (sitio == null) return NotFound();

        var admin = await _authService.ValidarAdminSitioAsync(model.Email, model.Password, sitio.Id);

        if (admin == null)
        {
            ModelState.AddModelError("", "Credenciales inválidas para este sitio.");
            return View(model);
        }

        Response.Cookies.Append($"AuthToken_{sitio.Id}", "token_generado", new CookieOptions
        {
            HttpOnly = true,
            Secure = true
        });

        return RedirectToAction("Index", "AdminSitioAuth", new { slug = slug });
    }

    [HttpGet("Index/{slug}")]
    public async Task<IActionResult> Index(string slug)
    {
        // Solución: Especificamos <Sitio>
        var sitio = await _context.Sitios.FirstOrDefaultAsync<Sitio>(s => s.Slug == slug);
        if (sitio == null) return NotFound();

        return View(sitio);
    }
}