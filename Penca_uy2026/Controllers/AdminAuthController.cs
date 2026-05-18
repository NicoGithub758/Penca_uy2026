using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;
using Penca_uy2026.Data;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet("VerSitios")]
        public async Task<IActionResult> VerSitios()
        {
            // Buscamos la lista de todos los sitios usando tu MyDbContext
            var listaSitios = await _context.Sitios.ToListAsync();

            // Retorna la vista VerSitios.cshtml pasándole los datos
            return View(listaSitios);
        }

        // GET: /AdminAuth/EditarSitio/5
        [HttpGet("EditarSitio/{id}")]
        public async Task<IActionResult> EditarSitio(int id)
        {
            var sitio = await _context.Sitios.FindAsync(id);
            if (sitio == null) return NotFound();

            // Puedes pasar el mismo sitio o mapearlo a un ViewModel específico de edición
            return View(sitio);
        }

        // POST: /AdminAuth/EditarSitio/5
        [HttpPost("EditarSitio/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarSitio(int id, Sitio sitioActualizado)
        {
            if (id != sitioActualizado.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                try
                {
                    sitioActualizado.Slug = GenerarSlug(sitioActualizado.Nombre);

                    _context.Update(sitioActualizado);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"El sitio '{sitioActualizado.Nombre}' fue actualizado con éxito.";

                    // Redirección explícita segura para producción
                    return RedirectToAction("VerSitios", "AdminAuth");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                }
            }
            return View(sitioActualizado);
        }

        // POST: /AdminAuth/EliminarSitio/5
        [HttpPost("EliminarSitio/{id}")]
        // [ValidateAntiForgeryToken] // Comentado temporalmente para evitar bloqueos por reinicios de Railway
        public async Task<IActionResult> EliminarSitio(int id)
        {
            // 1. Buscamos el sitio/tenant que se quiere borrar
            var sitio = await _context.Sitios.FindAsync(id);
            if (sitio == null)
            {
                return NotFound();
            }

            try
            {
                // 2. Limpiamos todas las tablas dependientes que apuntan a este SitioId
                // Así evitamos el error de Foreign Key en SQL Server

                var invitaciones = _context.Invitaciones.Where(i => i.SitioId == id);
                _context.Invitaciones.RemoveRange(invitaciones);

                var solicitudes = _context.SolicitudesIngreso.Where(s => s.SitioId == id);
                _context.SolicitudesIngreso.RemoveRange(solicitudes);

                var instanciasPencas = _context.PencaInstancias.Where(pi => pi.SitioId == id);
                _context.PencaInstancias.RemoveRange(instanciasPencas);

                var usuarios = _context.UsuariosSitio.Where(u => u.SitioId == id);
                _context.UsuariosSitio.RemoveRange(usuarios);

                // 3. Ahora que no hay registros hijos, removemos el Sitio
                _context.Sitios.Remove(sitio);

                // 4. Guardamos todo en una sola transacción en la Base de Datos
                await _context.SaveChangesAsync();

                TempData["Success"] = $"El sitio '{sitio.Nombre}' y todos sus datos asociados se eliminaron correctamente.";
            }
            catch (Exception ex)
            {
                // Si llega a quedar alguna otra tabla vinculada, el error saltará acá y lo verás en pantalla
                TempData["Error"] = "Error al eliminar en la Base de Datos: " + (ex.InnerException?.Message ?? ex.Message);
            }

            // Redirección explícita segura a la tabla
            return RedirectToAction("VerSitios", "AdminAuth");
        }
    }
}