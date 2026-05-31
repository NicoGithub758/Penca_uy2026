using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Penca_uy2026.Controllers
{
    [Authorize(Roles = "PlataformaAdmin")]
    public class ParametrosSistemaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
