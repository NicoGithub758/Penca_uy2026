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

        // Este muestra la página (GET)
        [HttpGet("Login")]
        public IActionResult Login() => View(new LoginViewModel());

        // Este recibe los datos al apretar el botón (POST)
        [HttpPost("Login")] // <--- ESTO ES LO QUE TE FALTA O ESTÁ MAL
        public IActionResult Login(LoginRequest request)
        {
            var token = _authService.LoginPlataforma(request);

            if (token == null)
            {
                ViewBag.Error = "Email o contraseña incorrectos.";
                return View(new LoginViewModel { Email = request.Email });
            }

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(8)
            });

            // Cambiamos el redireccionamiento para que sea exacto
            return RedirectToAction("Index", "Penca");
        }

        // GET: /AdminAuth/CrearSitio (Muestra el formulario)
        [HttpGet("CrearSitio")]
        public IActionResult CrearSitio()
        {
            return View(new CrearSitioViewModel());
        }

        // POST: /AdminAuth/CrearSitio (Recibe los datos del formulario)
        [HttpPost("CrearSitio")]
        [ValidateAntiForgeryToken] // Seguridad obligatoria
        public async Task<IActionResult> CrearSitio(CrearSitioViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Creamos el Sitio
                var nuevoSitio = new Sitio
                {
                    Nombre = model.NombreSitio,
                    Url = model.UrlVercel.ToLower().Trim(),
                    Activo = true
                };

                _context.Sitios.Add(nuevoSitio);
                await _context.SaveChangesAsync();

                // 2. Creamos el Administrador del Sitio
                var adminSitio = new UsuarioSitio
                {
                    Nombre = model.NombreAdmin,
                    Email = model.EmailAdmin,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordAdmin),
                    EsAdminSitio = true,
                    SitioId = nuevoSitio.Id
                };

                _context.UsuariosSitio.Add(adminSitio);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] = "¡Sitio creado con éxito!";
                return RedirectToAction("Login");
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