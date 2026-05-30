namespace Penca_uy2026.DTOs
{
    /// <summary>
    /// Representa una penca instancia del sitio para mostrar en mobile.
    /// Incluye info de si el usuario participa o no.
    /// </summary>
    public class PencaInstanciaMobileDTO
    {
        public int PencaInstanciaId { get; set; }
        public int PencaId { get; set; }
        public string NombrePenca { get; set; } = string.Empty;
        public string Deporte { get; set; } = string.Empty;
        public int CantidadEquipos { get; set; }
        public decimal Costo { get; set; }
        public bool Finalizada { get; set; }

        // Info de participación del usuario actual
        public bool Participa { get; set; }
        public int? ParticipacionId { get; set; }
        public bool EstaPagado { get; set; }
        public int PuntajeTotal { get; set; }
    }
}
