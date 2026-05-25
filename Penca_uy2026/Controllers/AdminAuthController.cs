using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Interfaces;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    [Route("AdminAuth")]
    public class AdminAuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly MyDbContext _context;
        private readonly IEmailServicio _emailServicio;
        public AdminAuthController(AuthService authService, MyDbContext context, IEmailServicio emailServicio)
        {
            _authService = authService;
            _context = context;
            _emailServicio = emailServicio;
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
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CrearSitio(CrearSitioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Crear el Sitio (Tenant) sin tocar datos de diseño por ahora
                var nuevoSitio = new Sitio
                {
                    Nombre = model.NombreSitio,
                    Url = model.UrlVercel.Trim().ToLower(),
                    Activo = true,
                    TipoRegistro = model.TipoRegistro,
                    Slug = model.NombreSitio.ToLower().Trim().Replace(" ", "-"),
                    ColorPrincipal = "#000000", // Valores por defecto
                    Descripcion = "",
                    LogoUrl = ""
                };

                _context.Sitios.Add(nuevoSitio);
                await _context.SaveChangesAsync(); // Genera el nuevoSitio.Id

                // 2. Crear el Administrador del Sitio con PasswordHash en NULL
                var adminSitio = new UsuarioSitio
                {
                    Nombre = model.NombreAdmin.Trim(),
                    Email = model.EmailAdmin.Trim().ToLower(),
                    PasswordHash = null, // No maneja contraseña inicial, queda pendiente
                    SitioId = nuevoSitio.Id,
                    Rol = RolUsuarioSitio.AdminSitio,
                    Activo = true,
                    FechaRegistro = DateTime.UtcNow
                };

                _context.UsuariosSitio.Add(adminSitio);
                await _context.SaveChangesAsync(); // Genera el adminSitio.Id

                // 3. Generar el Token único y temporal para la invitación
                string tokenSeguro = Guid.NewGuid().ToString("N"); // Crea un string alfanumérico aleatorio y limpio

                var invitacion = new InvitacionAdmin
                {
                    Token = tokenSeguro,
                    UsuarioSitioId = adminSitio.Id,
                    FechaExpiracion = DateTime.UtcNow.AddHours(48), // El link expira en 48 horas
                    Usado = false
                };

                _context.InvitacionesAdmin.Add(invitacion);
                await _context.SaveChangesAsync();

                // 4. Comprometer la transacción en la BD local/Azure
                await transaction.CommitAsync();

                // 5. ENVIAR EL CORREO ELECTRÓNICO (Usamos Request.Host para saber la URL actual corriendo de forma dinámica)
                try
                {
                    string urlActual = Request.Host.Value; // Captura si estás en localhost:xxxx o en tu dominio de Railway
                    await _emailServicio.EnviarEmailInvitacionAsync(adminSitio.Email, adminSitio.Nombre, tokenSeguro, urlActual);
                }
                catch (Exception ex)
                {
                    // Si el mail falla, dejamos registro en consola de Railway pero no le rompemos la pantalla al usuario, el sitio ya se creó.
                    Console.WriteLine($"[ERROR SMTP] No se pudo despachar el correo: {ex.Message}");
                }

                TempData["Success"] = $"Sitio '{nuevoSitio.Nombre}' registrado. Se enviará un correo de configuración a {adminSitio.Email}.";
                return RedirectToAction("VerSitios", "AdminAuth");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error crítico al procesar el alta: " + (ex.InnerException?.Message ?? ex.Message));
                return View(model);
            }
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