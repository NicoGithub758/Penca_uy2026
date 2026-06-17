using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Configuracion general del sitio. Cada sitio tiene UNA fila.
    /// Si no existe, se crea con valores default al pedirla.
    /// 
    /// Aca van todos los parametros configurables del sitio:
    ///   - Recordatorios automaticos de partidos
    ///   - (futuro) otros parametros como idioma, moneda, etc.
    /// </summary>
    public class ConfiguracionSitio : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        public int SitioId { get; set; }

        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;

        // ----- RECORDATORIOS AUTOMATICOS DE PARTIDOS -----

        /// <summary>
        /// Si esta activo, el background service envia recordatorios automaticos
        /// antes de cada partido a los participantes que NO predijeron aun.
        /// </summary>
        public bool RecordatoriosAutomaticosActivos { get; set; } = true;

        /// <summary>
        /// Cuantas horas antes del partido enviar el recordatorio automatico.
        /// Valores tipicos: 1, 3, 6, 12, 24, 48.
        /// </summary>
        [Range(1, 72)]
        public int HorasAntes { get; set; } = 1;

    }
}
