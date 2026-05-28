using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Models;
using Penca_uy2026.DTOs;

namespace Penca_uy2026.Services
{
    /// <summary>
    /// Servicio encargado de la lógica de negocio para la gestión de invitaciones.
    /// Separa las reglas de dominio de la capa de presentación (Controladores).
    /// </summary>
    public class InvitacionService
    {
        private readonly MyDbContext _context;

        public InvitacionService(MyDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Valida las condiciones del sitio y genera un token único de invitación.
        /// </summary>
        public async Task<(GenerarInvitacionResponse? Response, string? ErrorMessage)> GenerarInvitacionAsync(int tokenSitioId, GenerarInvitacionRequest request)
        {
            var sitio = await _context.Sitios.FirstOrDefaultAsync(s => s.Slug == request.Slug);
            
            if (sitio == null)
            {
                return (null, "El sitio especificado no existe.");
            }

            if (tokenSitioId != sitio.Id)
            {
                return (null, "FORBIDDEN");
            }

            if (sitio.TipoRegistro != TipoRegistro.SoloConInvitacion)
            {
                return (null, "El sitio no permite registro por invitaciones.");
            }

            // Se utiliza Guid.NewGuid() para asegurar unicidad universal matemática.
            // Al aplicar .ToString("N"), se eliminan los guiones generados por defecto (ej. "d3f4..."), 
            // resultando en una cadena alfanumérica limpia de 32 caracteres, ideal para ser incrustada de forma limpia en las URLs de invitación enviadas a los usuarios.
            var nuevaInvitacion = new Invitacion
            {
                SitioId = sitio.Id,
                Email = request.Email ?? string.Empty,
                Token = Guid.NewGuid().ToString("N"),
                UsosDisponibles = 1
            };

            _context.Invitaciones.Add(nuevaInvitacion);
            await _context.SaveChangesAsync();

            return (new GenerarInvitacionResponse
            {
                Token = nuevaInvitacion.Token,
                Mensaje = "Enlace de invitación generado correctamente."
            }, null);
        }

        /// <summary>
        /// Valida si un token de invitación existe, pertenece al slug correcto, y tiene usos disponibles.
        /// </summary>
        public async Task<bool> ValidarInvitacionAsync(string token, string slug)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(slug)) return false;

            return await _context.Invitaciones
                .Include(i => i.Sitio)
                .AnyAsync(i => i.Token == token && i.Sitio.Slug == slug && i.UsosDisponibles > 0);
        }
    }
}
