using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 
using Penca_uy2026.Data;
using Penca_uy2026.Interfaces;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;
using Microsoft.Extensions.Configuration;

namespace Penca_uy2026.Controllers
{
    [Route("AdminAuth")]
    [Authorize(Roles = "PlataformaAdmin")] 
    public class AdminAuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly MyDbContext _context;
        private readonly IEmailServicio _emailServicio;
        private readonly ImageService _imageService;
        private readonly IConfiguration _configuration;

        public AdminAuthController(AuthService authService, MyDbContext context, IEmailServicio emailServicio, ImageService imageService, IConfiguration configuration)
        {
            _authService = authService;
            _context = context;
            _emailServicio = emailServicio;
            _imageService = imageService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet("~/")]
        public async Task<IActionResult> Root()
        {
            if (Request.Cookies.TryGetValue("SitioId_Admin", out var sitioIdCookie) &&
                int.TryParse(sitioIdCookie, out var sitioId))
            {
                var sitioExiste = await _context.Sitios
                    .IgnoreQueryFilters()
                    .AnyAsync(s => s.Id == sitioId && s.Activo);

                if (sitioExiste)
                {
                    return RedirectToAction("Index", "AdminSitioAuth");
                }

                Response.Cookies.Delete("SitioId_Admin");
            }

            return RedirectToAction("Login", "AdminSitioAuth");
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }


        [AllowAnonymous] // Acceso público para ver el login
        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("PlataformaAdmin"))
            {
                return RedirectToAction("Index", "AdminAuth");
            }
            return View(new LoginViewModel());
        }

        [AllowAnonymous] // Acceso público para enviar credenciales
        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginRequest request)
        {
            var token = _authService.LoginPlataforma(request);

            if (token == null)
            {
                ViewBag.Error = "Credenciales incorrectas para el Panel de Administración.";
                return View(new LoginViewModel { Email = request.Email });
            }

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

        [AllowAnonymous] // Permitir logout incluso si la sesión expiró
        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Login");
        }

        [HttpGet("CrearSitio")]
        public IActionResult CrearSitio() => View(new CrearSitioViewModel());

        [HttpPost("CrearSitio")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CrearSitio(CrearSitioViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string logoUrlResult = "";
                if (model.Logo != null)
                {
                    using var stream = model.Logo.OpenReadStream();
                    logoUrlResult = await _imageService.UploadImageAsync(stream, model.Logo.FileName, cropToSquare: false);
                }

                var frontendDomain = _configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173";
                var nuevoSitio = new Sitio
                {
                    Nombre = model.NombreSitio,
                    Url = $"{frontendDomain}/{model.Slug}", // URL dinámica en base al frontend
                    Activo = true,
                    TipoRegistro = model.TipoRegistro,
                    Slug = model.Slug.ToLower().Trim(),
                    ColorPrincipal = "#000000",
                    Descripcion = "",
                    LogoUrl = logoUrlResult
                };

                _context.Sitios.Add(nuevoSitio);
                await _context.SaveChangesAsync();

                var adminSitio = new UsuarioSitio
                {
                    Nombre = model.NombreAdmin.Trim(),
                    Email = model.EmailAdmin.Trim().ToLower(),
                    //PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // [TEMPORAL FIX] Para pruebas locales
                    PasswordHash = null,
                    SitioId = nuevoSitio.Id,
                    Rol = RolUsuarioSitio.AdminSitio,
                    Activo = true,
                    FechaRegistro = DateTime.UtcNow
                };

                _context.UsuariosSitio.Add(adminSitio);
                await _context.SaveChangesAsync();

                string tokenSeguro = Guid.NewGuid().ToString("N");
                var invitacion = new InvitacionAdmin
                {
                    Token = tokenSeguro,
                    UsuarioSitioId = adminSitio.Id,
                    FechaExpiracion = DateTime.UtcNow.AddHours(48),
                    Usado = false
                };

                _context.InvitacionesAdmin.Add(invitacion);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    await _emailServicio.EnviarEmailInvitacionAsync(adminSitio.Email, adminSitio.Nombre, tokenSeguro, Request.Host.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR SMTP] {ex.Message}");
                }

                TempData["Success"] = $"Sitio '{nuevoSitio.Nombre}' registrado.";
                return RedirectToAction("VerSitios", "AdminAuth");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet("VerSitios")]
        public async Task<IActionResult> VerSitios() => View(await _context.Sitios.ToListAsync());

        [HttpGet("EditarSitio/{id}")]
        public async Task<IActionResult> EditarSitio(int id)
        {
            var sitio = await _context.Sitios.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            return sitio == null ? NotFound() : View(sitio);
        }

        [HttpPost("EditarSitio/{id}")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EditarSitio(int id, Sitio sitioActualizado, IFormFile? logoFile)
        {
            var sitioOriginal = await _context.Sitios.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            if (sitioOriginal == null) return NotFound();

            sitioActualizado.LogoUrl = sitioOriginal.LogoUrl; // Lo mantenemos por defecto para la validación

            ModelState.Clear();
            TryValidateModel(sitioActualizado);

            if (!ModelState.IsValid) return View(sitioActualizado);

            // Subimos el logo si se envió uno nuevo
            if (logoFile != null && logoFile.Length > 0)
            {
                using var stream = logoFile.OpenReadStream();
                var newLogoUrl = await _imageService.UploadImageAsync(stream, logoFile.FileName, cropToSquare: false);
                if (!string.IsNullOrEmpty(newLogoUrl))
                {
                    sitioOriginal.LogoUrl = newLogoUrl;
                }
            }

            sitioOriginal.Nombre = sitioActualizado.Nombre;
            sitioOriginal.Activo = sitioActualizado.Activo;
            sitioOriginal.Slug = sitioActualizado.Slug?.ToLower().Trim() ?? sitioOriginal.Slug;
            
            var frontendDomain = _configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173";
            sitioOriginal.Url = $"{frontendDomain}/{sitioOriginal.Slug}";
            
            sitioOriginal.TipoRegistro = sitioActualizado.TipoRegistro;
            sitioOriginal.ColorPrincipal = sitioActualizado.ColorPrincipal;
            sitioOriginal.Descripcion = sitioActualizado.Descripcion;

            _context.Sitios.Update(sitioOriginal);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Actualizado correctamente.";
            return RedirectToAction("VerSitios", "AdminAuth");
        }

        [HttpPost("EliminarSitio/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarSitio(int id)
        {
            var sitio = await _context.Sitios.FindAsync(id);
            if (sitio == null) return NotFound();

            // Lógica de borrado en cascada manual
            var participaciones = await _context.Participaciones.IgnoreQueryFilters().Where(p => p.SitioId == id).ToListAsync();
            var pagos = await _context.Pagos.IgnoreQueryFilters().Where(p => p.SitioId == id).ToListAsync();
            var invitaciones = await _context.Invitaciones.IgnoreQueryFilters().Where(i => i.SitioId == id).ToListAsync();
            var solicitudes = await _context.SolicitudesIngreso.IgnoreQueryFilters().Where(s => s.SitioId == id).ToListAsync();
            var instancias = await _context.PencaInstancias.IgnoreQueryFilters().Where(pi => pi.SitioId == id).ToListAsync();
            var usuarios = await _context.UsuariosSitio.IgnoreQueryFilters().Where(u => u.SitioId == id).ToListAsync();
            var usuarioIds = usuarios.Select(u => u.Id).ToList();
            var predicciones = await _context.Predicciones.IgnoreQueryFilters().Where(p => p.SitioId == id).ToListAsync();
            var mensajesChat = await _context.MensajesChat.IgnoreQueryFilters().Where(m => m.SitioId == id).ToListAsync();
            var notificaciones = await _context.Notificaciones.IgnoreQueryFilters().Where(n => n.SitioId == id).ToListAsync();
            var preferencias = await _context.PreferenciasNotificacion.IgnoreQueryFilters().Where(p => p.SitioId == id).ToListAsync();
            var invitacionesAdmin = await _context.InvitacionesAdmin.Where(i => usuarioIds.Contains(i.UsuarioSitioId)).ToListAsync();
            var reglasPremios = await _context.ReglasPremios.IgnoreQueryFilters().Where(r => r.SitioId == id).ToListAsync();

            _context.Pagos.RemoveRange(pagos);
            _context.Predicciones.RemoveRange(predicciones);
            _context.MensajesChat.RemoveRange(mensajesChat);
            _context.Notificaciones.RemoveRange(notificaciones);
            _context.PreferenciasNotificacion.RemoveRange(preferencias);
            _context.InvitacionesAdmin.RemoveRange(invitacionesAdmin);
            _context.ReglasPremios.RemoveRange(reglasPremios);
            _context.Participaciones.RemoveRange(participaciones);
            _context.Invitaciones.RemoveRange(invitaciones);
            _context.SolicitudesIngreso.RemoveRange(solicitudes);
            _context.PencaInstancias.RemoveRange(instancias);
            _context.UsuariosSitio.RemoveRange(usuarios);
            _context.Sitios.Remove(sitio);

            await _context.SaveChangesAsync();
            return RedirectToAction("VerSitios", "AdminAuth");
        }

        [HttpGet("CrearAdmin")]
        public IActionResult CrearAdmin() => View(new RegistrarAdminViewModel());

        [HttpPost("CrearAdmin")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CrearAdmin(RegistrarAdminViewModel request)
        {
            if (!ModelState.IsValid) return View(request);

            var existe = await _context.PlataformaAdmins.AnyAsync(a => a.Email.ToLower() == request.Email.ToLower());
            if (existe) { ModelState.AddModelError("Email", "Ya registrado."); return View(request); }

            var nuevoAdmin = new PlataformaAdmin { Email = request.Email.Trim(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password) };
            _context.PlataformaAdmins.Add(nuevoAdmin);
            await _context.SaveChangesAsync();
            return RedirectToAction("VerSitios", "AdminAuth");
        }

        [HttpGet("Estadisticas")]
        public async Task<IActionResult> Estadisticas()
        {
            var model = new EstadisticasViewModel
            {
                TotalPencas = await _context.Pencas.IgnoreQueryFilters().CountAsync(),
                TotalUsuarios = await _context.UsuariosSitio.IgnoreQueryFilters().CountAsync(),
                DineroTotalIngresado = await _context.Pagos.IgnoreQueryFilters().SumAsync(p => (decimal?)p.Monto) ?? 0m,
                DeporteMasPopular = await _context.Pencas.IgnoreQueryFilters().GroupBy(p => p.Deporte.Nombre).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefaultAsync() ?? "Sin datos",
                EstadisticasPorSitio = await _context.Sitios.IgnoreQueryFilters().Select(s => new EstadisticaSitioDTO
                {
                    NombreSitio = s.Nombre,
                    CantidadPencas = s.PencaInstancias.Count,
                    DineroRecaudado = s.PencaInstancias.SelectMany(pi => pi.Participaciones).SelectMany(part => part.Pagos).Sum(p => (decimal?)p.Monto) ?? 0m,
                    CantidadAdmins = s.Usuarios.Count(u => u.Rol == RolUsuarioSitio.AdminSitio),
                    CantidadUsuarios = s.Usuarios.Count(u => u.Rol == RolUsuarioSitio.Jugador)
                }).ToListAsync()
            };
            return View(model);
        }
    }
}
