using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Models.ViewModels;

namespace Penca_uy2026.Controllers
{
    // Al no tener [Route("AdminAuth")], este controlador es independiente
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;

        public AccountController(MyDbContext context)
        {
            _context = context;
        }

        // GET: /Account/ConfigurarPassword?token=...
        [HttpGet]
        public async Task<IActionResult> ConfigurarPassword(string token)
        {
            var invitacion = await _context.InvitacionesAdmin
                                          .Include(i => i.UsuarioSitio)
                                          .FirstOrDefaultAsync(i => i.Token == token);

            if (invitacion == null || !invitacion.IsValido)
            {
                return View("ErrorInvitacion");
            }

            return View(new ConfirmarPasswordViewModel { Token = token });
        }

        // POST: /AdminAuth/ConfigurarPassword
        [HttpPost("ConfigurarPassword")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ConfigurarPassword(ConfirmarPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Volvemos a buscar la invitación con su usuario bajo una transacción
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invitacion = await _context.InvitacionesAdmin
                                              .Include(i => i.UsuarioSitio)
                                              .FirstOrDefaultAsync(i => i.Token == model.Token);

                if (invitacion == null || !invitacion.IsValido)
                {
                    ModelState.AddModelError("", "La invitación ya no es válida.");
                    return View(model);
                }

                var usuario = invitacion.UsuarioSitio;
                if (usuario == null)
                {
                    return NotFound();
                }

                // 1. Reemplazamos el NULL por el PasswordHash real usando BCrypt
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                _context.UsuariosSitio.Update(usuario);

                // 2. Quemamos el token para que nadie pueda volver a usar el link
                invitacion.Usado = true;
                _context.InvitacionesAdmin.Update(invitacion);

                // Guardamos todo e impactamos la BD
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "¡Contraseña configurada con éxito! Ya podés ingresar a tu panel.";
                return RedirectToAction("Login", "Home"); // Redireccionar al login oficial de la app
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Ocurrió un error al guardar la contraseña: " + ex.Message);
                return View(model);
            }
        }

    }
}