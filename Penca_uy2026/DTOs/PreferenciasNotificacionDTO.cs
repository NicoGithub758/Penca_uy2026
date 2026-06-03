namespace Penca_uy2026.DTOs
{
    /// <summary>
    /// Representa las preferencias de notificaciones de un usuario.
    /// Se usa tanto para devolver el estado actual como para actualizar.
    /// </summary>
    public class PreferenciasNotificacionDTO
    {
        public bool RecibirResultados { get; set; } = true;
        public bool RecibirPartidos { get; set; } = true;
        public bool RecibirGenerales { get; set; } = true;
        public bool RecibirRanking { get; set; } = true;
    }
}
