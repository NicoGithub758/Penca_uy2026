using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;

namespace Penca_uy2026.Services
{
    public class PreferenciasService
    {
        private readonly MyDbContext _context;
        private readonly ILogger<PreferenciasService> _logger;

        public PreferenciasService(MyDbContext context, ILogger<PreferenciasService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(PreferenciasNotificacionDTO? Data, string? ErrorMessage)> GetPreferenciasAsync(int usuarioId, int tokenSitioId, string slug)
        {
            var sitio = await _context.Sitios.FirstOrDefaultAsync(s => s.Slug == slug);
            if (sitio == null) return (null, "NOT_FOUND");

            if (sitio.Id != tokenSitioId) return (null, "FORBIDDEN");

            var prefs = await _context.PreferenciasNotificacion
                .FirstOrDefaultAsync(p => p.UsuarioSitioId == usuarioId);

            if (prefs == null)
            {
                prefs = new PreferenciaNotificacion
                {
                    UsuarioSitioId = usuarioId,
                    SitioId = sitio.Id
                    // El resto queda con sus valores default = true
                };
                _context.PreferenciasNotificacion.Add(prefs);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[PreferenciasService] Creadas defaults para usuario {usuarioId}");
            }

            var dto = new PreferenciasNotificacionDTO
            {
                RecibirResultados = prefs.RecibirResultados,
                RecibirPartidos = prefs.RecibirPartidos,
                RecibirGenerales = prefs.RecibirGenerales,
                RecibirRanking = prefs.RecibirRanking
            };

            return (dto, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdatePreferenciasAsync(int usuarioId, int tokenSitioId, string slug, PreferenciasNotificacionDTO request)
        {
            var sitio = await _context.Sitios.FirstOrDefaultAsync(s => s.Slug == slug);
            if (sitio == null) return (false, "NOT_FOUND");

            if (sitio.Id != tokenSitioId) return (false, "FORBIDDEN");

            var prefs = await _context.PreferenciasNotificacion
                .FirstOrDefaultAsync(p => p.UsuarioSitioId == usuarioId);

            if (prefs == null)
            {
                prefs = new PreferenciaNotificacion
                {
                    UsuarioSitioId = usuarioId,
                    SitioId = sitio.Id
                };
                _context.PreferenciasNotificacion.Add(prefs);
            }

            prefs.RecibirResultados = request.RecibirResultados;
            prefs.RecibirPartidos = request.RecibirPartidos;
            prefs.RecibirGenerales = request.RecibirGenerales;
            prefs.RecibirRanking = request.RecibirRanking;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"[PreferenciasService] Actualizadas para usuario {usuarioId}");

            return (true, null);
        }
    }
}
