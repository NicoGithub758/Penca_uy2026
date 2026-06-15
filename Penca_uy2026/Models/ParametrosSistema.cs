using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models
{
    public class ParametrosSistema
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string TimeZoneId { get; set; } = "America/Montevideo";

        public bool ActualizacionAutomaticaResultadosActiva { get; set; } = true;

        [Range(1, 300)]
        public int MinutosDespuesInicioParaConsultarResultado { get; set; } = 110;

        [Range(1, 1440)]
        public int IntervaloMinutosConsultaResultados { get; set; } = 10;

        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
