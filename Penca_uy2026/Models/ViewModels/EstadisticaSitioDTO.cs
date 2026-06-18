namespace Penca_uy2026.Models.ViewModels
{
    public class EstadisticaSitioDTO
    {
        public string NombreSitio { get; set; } = string.Empty;

        public int CantidadPencas { get; set; }

        public decimal DineroRecaudado { get; set; }

        public int CantidadAdmins { get; set; }

        public int CantidadUsuarios { get; set; }
    }
}