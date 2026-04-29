using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")] // Obligatorio en EF Core para evitar pérdida de precisión en dinero
        public decimal Amount { get; set; } // Amount: Monto pagado

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        // ExternalTransactionId: El código de recibo que nos devuelve MercadoPago/PayPal
        public string ExternalTransactionId { get; set; } = string.Empty;

        // --- Relaciones ---
        [Required]
        public int ParticipationId { get; set; }
        [ForeignKey("ParticipationId")]
        public Participation Participation { get; set; } = null!;
    }
}