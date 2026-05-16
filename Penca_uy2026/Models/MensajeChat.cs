using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Representa un mensaje enviado por un participante en el chat de una instancia de penca.
    /// </summary>
    public class MensajeChat : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Contenido { get; set; } = string.Empty;

        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

        // --- RELACIONES ---

        [Required]
        public int ParticipacionId { get; set; }

        public int SitioId { get; set; }

        [ForeignKey("ParticipacionId")]
        public Participacion Participacion { get; set; } = null!;
    }
}
