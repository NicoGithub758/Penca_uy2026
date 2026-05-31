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

        [HttpGet("~/")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            if (Request.Cookies.ContainsKey("AuthToken"))
            {
                return View();
            }

            return RedirectToAction(nameof(Login));
        }

        // GET: /AdminAuth/Login
        [HttpGet("Login")]
        public IActionResult Login()
        {
            // Si ya tiene el token, lo mandamos al panel
            if (Request.Cookies.ContainsKey("AuthToken"))
            {
                return RedirectToAction("Index", "AdminAuth");
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

            // Redirigir al panel principal del admin
            return RedirectToAction("Index", "AdminAuth");
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
                return RedirectToAction("Index", "AdminAuth");
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

            // 1. Convertir a minúsculas y normalizar (descompone caracteres como 'ñ' en 'n' + '~')
            string str = nombre.ToLower().Trim().Normalize(System.Text.NormalizationForm.FormD);

            // 2. Filtrar caracteres: dejamos la letra base y quitamos el acento/tilde
            var sb = new System.Text.StringBuilder();
            foreach (char c in str)
            {
                // Usamos UnicodeCategory (corregido)
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);

                // NonSpacingMark son los acentos, tildes y diéresis que queremos ignorar
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // 3. Limpieza final
            str = sb.ToString();
            // Quita cualquier cosa que no sea a-z o 0-9
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // Colapsa espacios múltiples en uno solo
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", " ").Trim();
            // Cambia espacios por guiones
            str = str.Replace(" ", "-");

            return str;
        }
    }
}
