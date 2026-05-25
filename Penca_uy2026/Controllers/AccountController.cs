using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Models.ViewModels;

namespace Penca_uy2026.Controllers
{
    // Al no tener [Route("AdminAuth")], este controlador es independiente
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
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ConfigurarPassword(string token)
        {
            // Buscamos la invitación
            var invitacion = await _context.InvitacionesAdmin
                                          .FirstOrDefaultAsync(i => i.Token == token);

            if (invitacion == null || invitacion.Usado)
            {
                return View("ErrorInvitacion");
            }

            // PASAMOS EL TOKEN AL MODELO PARA QUE EL HTML LO TENGA
            return View(new ConfirmarPasswordViewModel { Token = token });
        }

        // POST: /Account/ConfigurarPassword
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ConfigurarPassword(ConfirmarPasswordViewModel model)
        {
            // LOG DE DEBUG: ¿Qué está llegando realmente?
            Console.WriteLine($"DEBUG: Token recibido en POST: '{model.Token}'");

            if (string.IsNullOrEmpty(model.Token))
            {
                ModelState.AddModelError("", "El token no fue recibido por el servidor.");
                return View(model);
            }

            Console.WriteLine($"DEBUG POST: El token recibido es -> '{model.Token}'");

            if (string.IsNullOrEmpty(model.Token))
            {
                ModelState.AddModelError("", "Error: El token no llegó al servidor.");
                return View(model);
            }

            // Aquí buscamos con el token que SÍ sabemos que llegó
            var invitacion = await _context.InvitacionesAdmin
                                          .Include(i => i.UsuarioSitio)
                                          .FirstOrDefaultAsync(i => i.Token == model.Token);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // CORRECCIÓN: Usar model.Token y .Include(i => i.UsuarioSitio)
                var invitacion = await _context.InvitacionesAdmin
                                              .Include(i => i.UsuarioSitio)
                                              .FirstOrDefaultAsync(i => i.Token == model.Token);

                if (invitacion == null)
                {
                    Console.WriteLine("DEBUG: Token no encontrado en BD");
                    ModelState.AddModelError("", "Token no encontrado.");
                    return View(model);
                }

                if (invitacion.Usado)
                {
                    Console.WriteLine("DEBUG: El token ya fue usado");
                    ModelState.AddModelError("", "La invitación ya fue utilizada.");
                    return View(model);
                }

                var usuario = invitacion.UsuarioSitio;
                if (usuario == null)
                {
                    return NotFound("Usuario no encontrado.");
                }

                // 1. Hasheamos y actualizamos
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                _context.UsuariosSitio.Update(usuario);

                // 2. Quemamos el token
                invitacion.Usado = true;
                _context.InvitacionesAdmin.Update(invitacion);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "¡Contraseña configurada con éxito!";
                return RedirectToAction("Login", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(model);
            }
        }

    }
}