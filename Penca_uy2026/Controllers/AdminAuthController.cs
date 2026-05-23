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
            // Usamos IgnoreQueryFilters para que el administrador global siempre encuentre el sitio sin importar el tenant actual
            var sitio = await _context.Sitios
                                      .IgnoreQueryFilters()
                                      .FirstOrDefaultAsync(s => s.Id == id);

            if (sitio == null)
            {
                return NotFound();
            }

            return View(sitio);
        }
        // POST: /AdminAuth/EditarSitio/5
        [HttpPost("EditarSitio/{id}")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EditarSitio(int id, Sitio sitioActualizado)
        {
            if (id != sitioActualizado.Id)
            {
                return NotFound();
            }

            // 1. Buscamos el registro real original de inmediato ignorando filtros
            var sitioOriginal = await _context.Sitios
                                              .IgnoreQueryFilters()
                                              .FirstOrDefaultAsync(s => s.Id == id);

            if (sitioOriginal == null)
            {
                return NotFound();
            }

            // 2. CORRECCIÓN CRUCIAL: Le reasignamos el Slug original al objeto que viene del formulario
            // para que si el ModelState lo valida, vea que SI tiene valor y no falle.
            sitioActualizado.Slug = sitioOriginal.Slug;

            // Hacemos lo mismo con los otros datos técnicos por las dudas
            sitioActualizado.ColorPrincipal = sitioOriginal.ColorPrincipal;
            sitioActualizado.Descripcion = sitioOriginal.Descripcion;
            sitioActualizado.LogoUrl = sitioOriginal.LogoUrl;
            sitioActualizado.TipoRegistro = sitioOriginal.TipoRegistro;

            // 3. Ahora que parchamos los requeridos, limpiamos los errores previos y volvemos a validar
            ModelState.Clear();
            TryValidateModel(sitioActualizado);

            if (!ModelState.IsValid)
            {
                Console.WriteLine("--- [ERROR DE VALIDACIÓN PERSISTENTE] ---");
                foreach (var modelStateKey in ModelState.Keys)
                {
                    var value = ModelState[modelStateKey];
                    foreach (var error in value.Errors)
                    {
                        Console.WriteLine($"Propiedad: {modelStateKey} - Error: {error.ErrorMessage}");
                    }
                }
                return View(sitioActualizado);
            }

            try
            {
                // 4. Mapeamos los campos que el usuario SÍ editó en la pantalla
                sitioOriginal.Nombre = sitioActualizado.Nombre;
                sitioOriginal.Url = sitioActualizado.Url;
                sitioOriginal.Activo = sitioActualizado.Activo;

                // Actualizamos e impactamos
                _context.Sitios.Update(sitioOriginal);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"El sitio '{sitioOriginal.Nombre}' se actualizó correctamente.";
                return RedirectToAction("VerSitios", "AdminAuth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "No se pudieron guardar los cambios: " + (ex.InnerException?.Message ?? ex.Message));
                return View(sitioActualizado);
            }
        }

        [HttpPost("EliminarSitio/{id}")]
        // [ValidateAntiForgeryToken] // Seguimos dejándolo comentado para que Railway no moleste con cookies
        public async Task<IActionResult> EliminarSitio(int id)
        {
            var sitio = await _context.Sitios.FindAsync(id);
            if (sitio == null)
            {
                return NotFound();
            }

            try
            {
                // Agregamos .IgnoreQueryFilters() a cada consulta para asegurarnos de que limpie TODO en la BD

                var invitaciones = await _context.Invitaciones.IgnoreQueryFilters().Where(i => i.SitioId == id).ToListAsync();
                _context.Invitaciones.RemoveRange(invitaciones);

                var solicitudes = await _context.SolicitudesIngreso.IgnoreQueryFilters().Where(s => s.SitioId == id).ToListAsync();
                _context.SolicitudesIngreso.RemoveRange(solicitudes);

                var instanciasPencas = await _context.PencaInstancias.IgnoreQueryFilters().Where(pi => pi.SitioId == id).ToListAsync();
                _context.PencaInstancias.RemoveRange(instanciasPencas);

                var usuarios = await _context.UsuariosSitio.IgnoreQueryFilters().Where(u => u.SitioId == id).ToListAsync();
                _context.UsuariosSitio.RemoveRange(usuarios);

                // Ahora que barrimos todo usando IgnoreQueryFilters, removemos el Sitio libremente
                _context.Sitios.Remove(sitio);

                await _context.SaveChangesAsync();
                TempData["Success"] = $"El sitio '{sitio.Nombre}' y todos sus datos asociados se eliminaron correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar en la Base de Datos: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("VerSitios", "AdminAuth");
        }

        // GET: /AdminAuth/CrearAdmin
        [HttpGet("CrearAdmin")]
        public IActionResult CrearAdmin()
        {
            return View(new RegistrarAdminViewModel());
        }

        // POST: /AdminAuth/CrearAdmin
        [HttpPost("CrearAdmin")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CrearAdmin(RegistrarAdminViewModel request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var existe = await _context.PlataformaAdmins
                                           .AnyAsync(a => a.Email.ToLower() == request.Email.ToLower());

                if (existe)
                {
                    ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
                    return View(request);
                }

                // Hasheamos de forma segura con BCrypt
                string hashSeguro = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var nuevoAdmin = new PlataformaAdmin
                {
                    Email = request.Email.Trim(),
                    PasswordHash = hashSeguro
                };

                _context.PlataformaAdmins.Add(nuevoAdmin);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"El administrador '{nuevoAdmin.Email}' fue creado con éxito.";
                return RedirectToAction("VerSitios", "AdminAuth");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error en la Base de Datos: " + (ex.InnerException?.Message ?? ex.Message));
                return View(request);
            }
        }
    }
}