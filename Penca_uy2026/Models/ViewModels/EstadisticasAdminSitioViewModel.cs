namespace Penca_uy2026.Models.ViewModels
{
    public class EstadisticasAdminSitioViewModel
    {
        // KPIs
        public int TotalUsuarios { get; set; }
        public decimal DineroRecaudado { get; set; }
        public int TotalPencasActivas { get; set; }

        // Gráfico 1 – Ingresos mensuales
        public List<IngresoMensualSitioDTO> IngresosMensuales { get; set; } = new();

        // Gráfico 2 – Pencas con más jugadores
        public List<PencaJugadoresDTO> PencasPorJugadores { get; set; } = new();

        // Gráfico 3 – Pencas que más recaudaron
        public List<PencaRecaudacionDTO> PencasPorRecaudacion { get; set; } = new();

        // Gráfico 4 – Usuarios con / sin cuenta mobile (FcmToken)
        public int UsuariosConMobile { get; set; }
        public int UsuariosSinMobile { get; set; }

        // Gráfico 5 – Pencas con más predicciones
        public List<PencaPrediccionesDTO> PencasPorPredicciones { get; set; } = new();
    }

    // Nombre distinto al IngresoMensualDTO global para evitar ambigüedad
    public class IngresoMensualSitioDTO
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public decimal Monto { get; set; }
    }

    public class PencaJugadoresDTO
    {
        public string NombrePenca { get; set; } = string.Empty;
        public int CantidadJugadores { get; set; }
    }

    public class PencaRecaudacionDTO
    {
        public string NombrePenca { get; set; } = string.Empty;
        public decimal MontoRecaudado { get; set; }
    }

    public class PencaPrediccionesDTO
    {
        public string NombrePenca { get; set; } = string.Empty;
        public int CantidadPredicciones { get; set; }
    }
}

