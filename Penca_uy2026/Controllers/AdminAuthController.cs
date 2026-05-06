using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Models;
using Penca_uy2026.Models.ViewModels;
using Penca_uy2026.Services;

namespace Penca_uy2026.Controllers
{
    [Route("AdminAuth")]
    public class AdminAuthController : Controller
    {
        private readonly AuthService _authService;

        public AdminAuthController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: /AdminAuth/Login
        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (Request.Cookies.ContainsKey("AuthToken"))
            {
                return RedirectToAction("Index", "Penca", null);
            }

            // Enviamos un modelo vacío pero instanciado para evitar NullReference
            return View(new LoginViewModel());
        }

        // POST: /AdminAuth/Login
        [HttpPost("Login")]
        public IActionResult Login(LoginRequest request)
        {
            var token = _authService.LoginPlataforma(request);

            if (token == null)
            {
                ViewBag.Error = "Email o contraseña incorrectos.";
                // Retornamos el modelo con el Email para que el usuario no tenga que reescribirlo
                return View(new LoginViewModel { Email = request.Email });
            }

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(8)
            });

            return RedirectToAction("Index", "Penca", null);
        }

        [HttpGet("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Login");
        }

        [HttpGet("CrearSitio")]
        public IActionResult CrearSitio()
        {
            // Retornamos la vista con un modelo vacío para que no explote
            return View(new CrearSitioViewModel());
        }
    }
}