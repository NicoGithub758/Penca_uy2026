using Microsoft.AspNetCore.Mvc;

namespace Penca_uy2026.Controllers
{
    // Asegúrate de que NO tenga [ApiController] aquí arriba
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}