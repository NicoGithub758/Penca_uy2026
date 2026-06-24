namespace Penca_uy2026.Models.ViewModels
{
    public class EstadisticasViewModel
    {
        public int TotalPencas { get; set; }
        public decimal DineroTotalIngresado { get; set; }
        public int TotalUsuarios { get; set; }
        public string DeporteMasPopular { get; set; } = "N/A";
        public List<EstadisticaSitioDTO> EstadisticasPorSitio { get; set; } = new();

        // Esta es la nueva propiedad
        public List<IngresoMensualDTO> IngresosMensuales { get; set; } = new();

        public List<EvolucionUsuarioDTO> EvolucionUsuarios { get; set; } = new();

        public Dictionary<string, int> PagosPorMetodo { get; set; } = new();
        public int TotalAdmins { get; set; }

        public List<PencaPopularDTO> PencasMasPopulares { get; set; } = new();
        public List<PencaConUsuariosDTO> PencasConMasUsuarios { get; set; } = new();
        public List<UsuarioActivoDTO> UsuariosActivosUltimas48h { get; set; } = new();


    }

    public class IngresoMensualDTO
    {
        public string NombreSitio { get; set; } = string.Empty;
        public int Mes { get; set; }
        public int Anio { get; set; }
        public decimal Monto { get; set; }
    }

    public class EvolucionUsuarioDTO
    {
        public string NombreSitio { get; set; } = string.Empty;
        public string NombrePenca { get; set; } = string.Empty;
        public int Anio { get; set; }
        public int Mes { get; set; }
        public int CantidadUsuarios { get; set; }
    }

    public class PencaPopularDTO
    {
        public string NombrePenca { get; set; } = string.Empty;
        public string Deporte { get; set; } = string.Empty;
        public int CantidadInstancias { get; set; }
    }
    public class PencaConUsuariosDTO
    {
        public string NombrePenca { get; set; } = string.Empty;
        public string NombreSitio { get; set; } = string.Empty;
        public string Deporte { get; set; } = string.Empty;
        public int CantidadUsuarios { get; set; }
    }
    public class UsuarioActivoDTO
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombreSitio { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NombrePenca { get; set; } = string.Empty;
        public int CantidadPredicciones { get; set; }
        public DateTime UltimaPrediccion { get; set; }
    }

}