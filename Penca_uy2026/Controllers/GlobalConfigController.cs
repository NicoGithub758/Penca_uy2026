using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace Penca_uy2026;


[ApiController]
[Route("api/configuracion-global")]
[Authorize(Roles = "PlataformaAdmin")] // Solo permite a los que tengan este rol en el token
public class GlobalConfigController : ControllerBase
{
    [HttpGet]
    public IActionResult GetConfig() => Ok("Datos sensibles de la plataforma");
}