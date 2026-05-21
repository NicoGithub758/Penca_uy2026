using Microsoft.AspNetCore.Mvc.Rendering;

namespace Penca_uy2026.Models.ViewModels
{
    public class PencaViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public int DeporteId { get; set; }
        public int CantidadEquipos { get; set; }
        public ModoPenca Modo { get; set; }
        public int? ApiFootballLeagueId { get; set; }
        public int? ApiFootballSeason { get; set; }

        // Para el desplegable
        public List<SelectListItem>? Deportes { get; set; }
    }
}