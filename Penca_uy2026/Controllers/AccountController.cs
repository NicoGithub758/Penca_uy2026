using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;
        private readonly AuthService _authService;

        public AccountController(MyDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> ConfigurarPassword(string token)
        {
            // Usamos IgnoreQueryFilters por si un filtro global está ocultando invitaciones pendientes
            var invitacion = await _context.InvitacionesAdmin
                                          .IgnoreQueryFilters()
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
            // Validación básica de entrada
            if (string.IsNullOrEmpty(model.Token))
            {
                ModelState.AddModelError("", "El token no es válido.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Buscamos la invitación ignorando filtros globales que pudieran bloquearla
                var invitacion = await _context.InvitacionesAdmin
                                              .IgnoreQueryFilters()
                                              .Include(i => i.UsuarioSitio)
                                              .FirstOrDefaultAsync(i => i.Token == model.Token);

                if (invitacion == null)
                {
                    ModelState.AddModelError("", "No se encontró una invitación válida con ese token.");
                    return View(model);
                }

                if (invitacion.Usado)
                {
                    ModelState.AddModelError("", "Esta invitación ya fue utilizada anteriormente.");
                    return View(model);
                }

                var usuario = invitacion.UsuarioSitio;
                if (usuario == null)
                {
                    ModelState.AddModelError("", "No se pudo asociar la cuenta de usuario.");
                    return View(model);
                }

                // 1. Actualizamos el password
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                _context.UsuariosSitio.Update(usuario);

                // 2. Marcamos el token como usado
                invitacion.Usado = true;
                _context.InvitacionesAdmin.Update(invitacion);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Redirigimos al Login de administración
                return RedirectToAction("Login", "AdminSitioAuth");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error al procesar la solicitud: " + ex.Message);
                return View(model);
            }
        }
    }
}
