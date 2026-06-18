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

        [Required(ErrorMessage = "El costo de entrada es obligatorio.")]
        [Range(0, 10000, ErrorMessage = "El costo debe ser mayor o igual a 0.")]
        public decimal Costo { get; set; }
    }
}