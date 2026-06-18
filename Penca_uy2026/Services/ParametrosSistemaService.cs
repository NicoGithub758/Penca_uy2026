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
                PuntosResultadoExacto = 8,
                PuntosGanadorDiferenciaGoles = 5,
                PuntosGanadorEmpate = 3,
                PorcentajeComisionPenca = 5,
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

            var erroresPuntajes = ValidarPuntajes(parametrosActualizados);
            if (erroresPuntajes.Any())
                throw new ArgumentException(string.Join(" ", erroresPuntajes.Select(e => e.Mensaje)));

            var erroresComision = ValidarComision(parametrosActualizados);
            if (erroresComision.Any())
                throw new ArgumentException(string.Join(" ", erroresComision.Select(e => e.Mensaje)));

            var parametros = await ObtenerAsync(cancellationToken);

            parametros.TimeZoneId = parametrosActualizados.TimeZoneId;
            parametros.ActualizacionAutomaticaResultadosActiva = parametrosActualizados.ActualizacionAutomaticaResultadosActiva;
            parametros.MinutosDespuesInicioParaConsultarResultado = parametrosActualizados.MinutosDespuesInicioParaConsultarResultado;
            parametros.IntervaloMinutosConsultaResultados = parametrosActualizados.IntervaloMinutosConsultaResultados;
            parametros.PuntosResultadoExacto = parametrosActualizados.PuntosResultadoExacto;
            parametros.PuntosGanadorDiferenciaGoles = parametrosActualizados.PuntosGanadorDiferenciaGoles;
            parametros.PuntosGanadorEmpate = parametrosActualizados.PuntosGanadorEmpate;
            parametros.PorcentajeComisionPenca = parametrosActualizados.PorcentajeComisionPenca;
            parametros.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public static List<(string Campo, string Mensaje)> ValidarPuntajes(ParametrosSistema parametros)
        {
            var errores = new List<(string Campo, string Mensaje)>();

            if (parametros.PuntosGanadorDiferenciaGoles > parametros.PuntosResultadoExacto)
            {
                errores.Add((
                    nameof(ParametrosSistema.PuntosGanadorDiferenciaGoles),
                    "El puntaje por ganador y diferencia de goles no puede ser mayor que el puntaje por resultado exacto."
                ));
            }

            if (parametros.PuntosGanadorEmpate > parametros.PuntosGanadorDiferenciaGoles)
            {
                errores.Add((
                    nameof(ParametrosSistema.PuntosGanadorEmpate),
                    "El puntaje por ganador/empate no puede ser mayor que el puntaje por ganador y diferencia de goles."
                ));
            }

            return errores;
        }

        public static List<(string Campo, string Mensaje)> ValidarComision(ParametrosSistema parametros)
        {
            var errores = new List<(string Campo, string Mensaje)>();

            if (parametros.PorcentajeComisionPenca < 0 || parametros.PorcentajeComisionPenca > 100)
            {
                errores.Add((
                    nameof(ParametrosSistema.PorcentajeComisionPenca),
                    "El porcentaje de comision debe estar entre 0 y 100."
                ));
            }

            return errores;
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
