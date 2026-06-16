using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.Models;

namespace Penca_uy2026.Services
{
    public class ParametrosSistemaService
    {
        private const int ParametrosSistemaId = 1;
        private const string TimeZonePorDefecto = "America/Montevideo";

        private readonly MyDbContext _context;

        public ParametrosSistemaService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<ParametrosSistema> ObtenerAsync(CancellationToken cancellationToken = default)
        {
            var parametros = await _context.ParametrosSistema
                .FirstOrDefaultAsync(p => p.Id == ParametrosSistemaId, cancellationToken);

            if (parametros != null)
                return parametros;

            parametros = new ParametrosSistema
            {
                Id = ParametrosSistemaId,
                TimeZoneId = TimeZonePorDefecto,
                ActualizacionAutomaticaResultadosActiva = true,
                MinutosDespuesInicioParaConsultarResultado = 110,
                IntervaloMinutosConsultaResultados = 10,
                FechaActualizacion = DateTime.UtcNow
            };

            _context.ParametrosSistema.Add(parametros);
            await _context.SaveChangesAsync(cancellationToken);

            return parametros;
        }

        public async Task ActualizarAsync(ParametrosSistema parametrosActualizados, CancellationToken cancellationToken = default)
        {
            if (!EsTimeZoneValido(parametrosActualizados.TimeZoneId))
                throw new ArgumentException("El huso horario seleccionado no es valido.");

            var parametros = await ObtenerAsync(cancellationToken);

            parametros.TimeZoneId = parametrosActualizados.TimeZoneId;
            parametros.ActualizacionAutomaticaResultadosActiva = parametrosActualizados.ActualizacionAutomaticaResultadosActiva;
            parametros.MinutosDespuesInicioParaConsultarResultado = parametrosActualizados.MinutosDespuesInicioParaConsultarResultado;
            parametros.IntervaloMinutosConsultaResultados = parametrosActualizados.IntervaloMinutosConsultaResultados;
            parametros.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<TimeZoneInfo> ObtenerTimeZoneInfoAsync(CancellationToken cancellationToken = default)
        {
            var parametros = await ObtenerAsync(cancellationToken);
            return ObtenerTimeZoneInfo(parametros.TimeZoneId);
        }

        public static bool EsTimeZoneValido(string timeZoneId)
        {
            try
            {
                _ = ObtenerTimeZoneInfo(timeZoneId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static TimeZoneInfo ObtenerTimeZoneInfo(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
            }
        }
    }
}
