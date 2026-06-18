using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Registro de que se envio recordatorio automatico para un partido en un sitio especifico.
    /// 
    /// IMPORTANTE: Esta tabla existe porque Partido es global (creado por AdminPlataforma)
    /// pero cada sitio tiene su propio HorasAntes. Sin esta tabla, no podriamos saber
    /// si el Sitio A ya notifico para el partido X mientras que el Sitio B no.
    /// 
    /// Si existe una fila (PartidoId=X, SitioId=Y), ese partido YA se notifico para ese sitio.
    /// </summary>
    public class RecordatorioPartidoSitio : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        public int PartidoId { get; set; }

        [ForeignKey("PartidoId")]
        public Partido Partido { get; set; } = null!;

        public int SitioId { get; set; }

        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;

        /// <summary>
        /// Cuando se envio el recordatorio (timestamp).
        /// </summary>
        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Cuantos usuarios recibieron el push (para logs/stats).
        /// </summary>
        public int CantidadEnviados { get; set; } = 0;
    }
}
