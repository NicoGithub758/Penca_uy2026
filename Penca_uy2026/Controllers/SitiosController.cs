using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Data;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;

[Authorize(Roles = "PlataformaAdmin")]
public class SitiosController : Controller
{
    private readonly MyDbContext _context;

    public SitiosController(MyDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearConAdmin(CrearSitioViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Iniciamos una transacción manual
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Crear el Sitio
            var nuevoSitio = new Sitio
            {
                Nombre = model.NombreSitio,
                Url = model.UrlVercel.ToLower()
            };

            _context.Sitios.Add(nuevoSitio);
            await _context.SaveChangesAsync(); // Guardamos para obtener el nuevoSitio.Id

            // 2. Crear el Admin del Sitio
            var adminSitio = new UsuarioSitio
            {
                Email = model.EmailAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordAdmin),
                EsAdminSitio = true,
                SitioId = nuevoSitio.Id // Lo vinculamos al sitio recién creado
            };

            _context.UsuariosSitio.Add(adminSitio);
            await _context.SaveChangesAsync();

            // Confirmamos todos los cambios en la BD
            await transaction.CommitAsync();

            TempData["Success"] = "Sitio y Administrador creados correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Ocurrió un error al crear el sitio.");
            return View(model);
        }
    }
}