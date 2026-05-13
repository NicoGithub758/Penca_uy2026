using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Penca_uy2026.Interfaces;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Representa la instanciación de una Penca global dentro de un Sitio específico.
    /// Permite personalizar comisiones y gestionar participaciones locales.
    /// </summary>
    public class PencaInstancia : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PorcentajeComision { get; set; } = 0;

        // --- RELACIONES ---

        [Required]
        public int PencaId { get; set; }
        
        [ForeignKey("PencaId")]
        public Penca Penca { get; set; } = null!;

        [Required]
        public int SitioId { get; set; }

        [ForeignKey("SitioId")]
        public Sitio Sitio { get; set; } = null!;

        /// <summary>
        /// Colección de usuarios que están participando en esta instancia de penca.
        /// </summary>
        public ICollection<Participacion> Participaciones { get; set; } = new List<Participacion>();
    }
}
