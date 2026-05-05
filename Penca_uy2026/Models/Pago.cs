using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Registro de una transacción monetaria para participar en una penca.
    /// </summary>
    public class Pago
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime FechaPago { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Código de recibo o ID devuelto por la pasarela de pago (MercadoPago/PayPal).
        /// </summary>
        public string IdTransaccionExterna { get; set; } = string.Empty;

        // --- RELACIONES ---
        
        [Required]
        public int ParticipacionId { get; set; }
        
        [ForeignKey("ParticipacionId")]
        public Participacion Participacion { get; set; } = null!;
    }
}
