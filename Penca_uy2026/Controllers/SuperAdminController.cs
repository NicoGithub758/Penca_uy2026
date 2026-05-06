using Microsoft.AspNetCore.Mvc;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;

[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "SuperAdmin")] // Descomenta cuando ya tengas los roles funcionando
public class SuperAdminController : ControllerBase
{
    private readonly MyDbContext _context; // Cambiado de ApplicationDbContext a MyDbContext

    public SuperAdminController(MyDbContext context)
    {
        _context = context;
    }

    [HttpPost("registrar-cliente")]
    public async Task<IActionResult> RegistrarNuevoCliente([FromBody] CrearSitioDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Crear el Sitio
            var nuevoSitio = new Sitio
            {
                Nombre = dto.NombreSitio,
                Url = dto.UrlVercel, // Asegúrate de que tu clase Sitio tenga esta propiedad
                Activo = true
            };

            _context.Sitios.Add(nuevoSitio);
            await _context.SaveChangesAsync();

            // 2. Crear el Administrador para ese sitio
            var adminSitio = new UsuarioSitio
            {
                Nombre = dto.NombreAdmin,
                Email = dto.EmailAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordAdmin),
                SitioId = nuevoSitio.Id,
                EsAdminSitio = true
            };

            _context.UsuariosSitio.Add(adminSitio);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return Ok(new { Message = "Sitio y Administrador creados correctamente", SitioId = nuevoSitio.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest($"Error al crear el sitio: {ex.Message}");
        }
    }
}