namespace Penca_uy2026.Models.ViewModels
{
    public class EstadisticasViewModel
    {
        public int TotalPencas { get; set; }
        public decimal DineroTotalIngresado { get; set; }
        public int TotalUsuarios { get; set; }
        public string DeporteMasPopular { get; set; } = "N/A";
        public List<EstadisticaSitioDTO> EstadisticasPorSitio { get; set; } = new();
    }

    public class EstadisticaSitioDTO
    {
        public string NombreSitio { get; set; } = string.Empty;
        public int CantidadPencas { get; set; }
        public decimal DineroRecaudado { get; set; }
        public int CantidadAdmins { get; set; }
        public int CantidadUsuarios { get; set; }
    }
}