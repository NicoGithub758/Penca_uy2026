using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class ReglaPremio : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        public int PencaInstanciaId { get; set; }

        [ForeignKey("PencaInstanciaId")]
        public PencaInstancia PencaInstancia { get; set; } = null!;

        [Range(1, 100)]
        public int Posicion { get; set; }

        [Range(0.01, 100.00)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeDelPozo { get; set; }

        public int SitioId { get; set; }
    }
}