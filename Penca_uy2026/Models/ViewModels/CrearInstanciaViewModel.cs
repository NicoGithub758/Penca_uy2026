using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models.ViewModels
{
    public class CrearInstanciaViewModel
    {
        [Required]
        public int PencaId { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal PorcentajeComision { get; set; }

        public decimal Costo { get; set; }
    }
}