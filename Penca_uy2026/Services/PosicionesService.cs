using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;

namespace Penca_uy2026.Services
{
    public class PosicionesService
    {
        private readonly MyDbContext _context;
        private readonly ILogger<PosicionesService> _logger;

        public PosicionesService(MyDbContext context, ILogger<PosicionesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la tabla de posiciones calculada para una Penca Instancia específica.
        /// </summary>
        public async Task<List<PosicionDTO>> ObtenerTablaPosicionesAsync(int pencaInstanciaId, int usuarioActualId)
        {
            try
            {
                // Obtener participaciones ordenadas por puntos
                var participaciones = await _context.Participaciones
                    .Include(p => p.UsuarioSitio)
                    .Where(p => p.PencaInstanciaId == pencaInstanciaId && p.EstaPagado == true)
                    .OrderByDescending(p => p.PuntajeTotal)
                    .ThenBy(p => p.UsuarioSitio.Nombre)
                    .ToListAsync();

                if (!participaciones.Any())
                    return new List<PosicionDTO>(); // Lista vacía si no hay participantes

                // Construir tabla con posiciones
                var tabla = new List<PosicionDTO>();
                int posicion = 1;
                int? puntosAnteriores = null;

                for (int i = 0; i < participaciones.Count; i++)
                {
                    var p = participaciones[i];

                    // Actualizar posición si los puntos cambiaron (los empates comparten posición)
                    if (puntosAnteriores.HasValue && p.PuntajeTotal < puntosAnteriores.Value)
                    {
                        posicion = i + 1;
                    }

                    tabla.Add(new PosicionDTO
                    {
                        Posicion = posicion,
                        UsuarioNombre = p.UsuarioSitio.Nombre,
                        Puntos = p.PuntajeTotal,
                        EsUsuarioActual = p.UsuarioSitioId == usuarioActualId
                    });

                    puntosAnteriores = p.PuntajeTotal;
                }

                return tabla;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PosicionesService] Error al obtener tabla: {ex.Message}");
                throw; // Relanzamos para que el controlador lo atrape y devuelva 500
            }
        }
    }
}
