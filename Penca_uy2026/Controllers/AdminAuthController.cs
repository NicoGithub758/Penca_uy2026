using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 
using Penca_uy2026.Data;
using Penca_uy2026.Interfaces;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    [Route("AdminAuth")]
    [Authorize] 
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

        [AllowAnonymous] // Acceso público para ver el login
        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (Request.Cookies.ContainsKey("AuthToken"))
            {
                return RedirectToAction("Index", "Penca");
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

            return RedirectToAction("Index", "Penca");
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
                var nuevoSitio = new Sitio
                {
                    Nombre = model.NombreSitio,
                    Url = model.UrlVercel.Trim().ToLower(),
                    Activo = true,
                    TipoRegistro = model.TipoRegistro,
                    Slug = model.NombreSitio.ToLower().Trim().Replace(" ", "-"),
                    ColorPrincipal = "#000000",
                    Descripcion = "",
                    LogoUrl = ""
                };

                _context.Sitios.Add(nuevoSitio);
                await _context.SaveChangesAsync();

                var adminSitio = new UsuarioSitio
                {
                    Nombre = model.NombreAdmin.Trim(),
                    Email = model.EmailAdmin.Trim().ToLower(),
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
        public async Task<IActionResult> EditarSitio(int id, Sitio sitioActualizado)
        {
            var sitioOriginal = await _context.Sitios.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            if (sitioOriginal == null) return NotFound();

            sitioActualizado.Slug = sitioOriginal.Slug;
            sitioActualizado.ColorPrincipal = sitioOriginal.ColorPrincipal;
            sitioActualizado.Descripcion = sitioOriginal.Descripcion;
            sitioActualizado.LogoUrl = sitioOriginal.LogoUrl;
            sitioActualizado.TipoRegistro = sitioOriginal.TipoRegistro;

            ModelState.Clear();
            TryValidateModel(sitioActualizado);

            if (!ModelState.IsValid) return View(sitioActualizado);

            sitioOriginal.Nombre = sitioActualizado.Nombre;
            sitioOriginal.Url = sitioActualizado.Url;
            sitioOriginal.Activo = sitioActualizado.Activo;

            _context.Sitios.Update(sitioOriginal);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Actualizado correctamente.";
            return RedirectToAction("VerSitios", "AdminAuth");
        }

        [HttpPost("EliminarSitio/{id}")]
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

            _context.Pagos.RemoveRange(pagos);
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