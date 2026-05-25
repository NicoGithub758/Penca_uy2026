using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Models.ViewModels;

namespace Penca_uy2026.Controllers
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;

        public AccountController(MyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ConfigurarPassword(string token)
        {
            var invitacion = await _context.InvitacionesAdmin
                                          .FirstOrDefaultAsync(i => i.Token == token);

            if (invitacion == null || invitacion.Usado)
            {
                return View("ErrorInvitacion");
            }

            return View(new ConfirmarPasswordViewModel { Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ConfigurarPassword(ConfirmarPasswordViewModel model)
        {
            Console.WriteLine($"DEBUG: Token recibido en POST: '{model.Token}'");

            if (string.IsNullOrEmpty(model.Token))
            {
                ModelState.AddModelError("", "El token no fue recibido.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Buscamos la invitación una sola vez
                var invitacion = await _context.InvitacionesAdmin
                                              .Include(i => i.UsuarioSitio)
                                              .FirstOrDefaultAsync(i => i.Token == model.Token);

                if (invitacion == null)
                {
                    ModelState.AddModelError("", "Token no encontrado en la base de datos.");
                    return View(model);
                }

                if (invitacion.Usado)
                {
                    ModelState.AddModelError("", "La invitación ya fue utilizada.");
                    return View(model);
                }

                var usuario = invitacion.UsuarioSitio;
                if (usuario == null)
                {
                    ModelState.AddModelError("", "Usuario asociado no encontrado.");
                    return View(model);
                }

                // 1. Actualizamos contraseña
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                _context.UsuariosSitio.Update(usuario);

                // 2. Marcamos como usado
                invitacion.Usado = true;
                _context.InvitacionesAdmin.Update(invitacion);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction("Login", "AdminAuth");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error interno: " + ex.Message);
                return View(model);
            }
        }
    }
}