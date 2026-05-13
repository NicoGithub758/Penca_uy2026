using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Data;

[Authorize(Roles = "PlataformaAdmin")]
public class SitiosController : Controller
{
    private readonly MyDbContext _context;

    public SitiosController(MyDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var sitios = _context.Sitios.ToList();
        return View(sitios);
    }
}