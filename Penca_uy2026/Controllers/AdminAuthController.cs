using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;
using Penca_uy2026.Data;

namespace Penca_uy2026.Controllers
{
    [Route("AdminAuth")]
    public class AdminAuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly MyDbContext _context;

        public AdminAuthController(AuthService authService, MyDbContext context)
        {
            _authService = authService;
            _context = context;
        }
        // GET: /AdminAuth/Login
        [HttpGet("Login")]
        public IActionResult Login()
        {
            // Si ya tiene el token, lo mandamos al panel
            if (Request.Cookies.ContainsKey("AuthToken"))
            {
                return RedirectToAction("Index", "Penca");
            }
            return View(new LoginViewModel());
        }

        // POST: /AdminAuth/Login
        [HttpPost("Login")]
        [ValidateAntiForgeryToken] // Fundamental para evitar el error 405 en producción
        public IActionResult Login(LoginRequest request)
        {
            var token = _authService.LoginPlataforma(request);

            if (token == null)
            {
                ViewBag.Error = "Credenciales incorrectas para el Panel de Administración.";
                return View(new LoginViewModel { Email = request.Email });
            }

            // Guardamos el token en una cookie segura
            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(8)
            });

            // Redirigir al Index del controlador Penca
            return RedirectToAction("Index", "Penca");
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Login");
        }
        // GET: /AdminAuth/CrearSitio (Muestra el formulario)
        [HttpGet("CrearSitio")]
        public IActionResult CrearSitio()
        {
            return View(new CrearSitioViewModel());
        }

        // POST: /AdminAuth/CrearSitio
        [HttpPost("CrearSitio")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearSitio(CrearSitioViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Generamos el Slug a partir del nombre
                string slugGenerado = GenerarSlug(model.NombreSitio);

                // 2. Creamos el Sitio con los nuevos campos
                var nuevoSitio = new Sitio
                {
                    Nombre = model.NombreSitio,
                    Url = model.UrlVercel.ToLower().Trim(),
                    Slug = slugGenerado, // <-- Campo nuevo
                    TipoRegistro = model.TipoRegistro, // <-- Tu nuevo Enum
                    Activo = true
                };

                _context.Sitios.Add(nuevoSitio);
                await _context.SaveChangesAsync();

                // 3. Creamos el Administrador del Sitio
                var adminSitio = new UsuarioSitio
                {
                    Nombre = model.NombreAdmin,
                    Email = model.EmailAdmin,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordAdmin),                  
                    SitioId = nuevoSitio.Id,
                    Rol = RolUsuarioSitio.AdminSitio
                };

                _context.UsuariosSitio.Add(adminSitio);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] = $"El sitio '{model.NombreSitio}' con slug '{slugGenerado}' ha sido creado.";
                return RedirectToAction("Index", "Penca");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                return View(model);
            }
        }

        // Función auxiliar para normalizar el Slug
        private string GenerarSlug(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return "sitio-sin-nombre";

            // Convertir a minúsculas, quitar espacios y caracteres especiales
            string str = nombre.ToLower().Trim();
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", " ").Trim();
            str = str.Replace(" ", "-");

            return str;
        }
    }
}