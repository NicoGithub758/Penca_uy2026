using Microsoft.AspNetCore.Mvc.Rendering;

namespace Penca_uy2026.Models.ViewModels
{
    public class PartidoViewModel
    {
        public int PencaId { get; set; }
        public string? PencaNombre { get; set; }

        public int LocalId { get; set; }
        public int VisitanteId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Fase { get; set; } = "Fase Regular";

        public List<SelectListItem>? Equipos { get; set; }
    }
}