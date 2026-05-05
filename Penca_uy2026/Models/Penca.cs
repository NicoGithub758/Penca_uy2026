namespace Penca_uy2026.Models
{
    public enum ModoPenca
    {
        Liga,                           // Todos contra todos
        FaseGruposEliminacion,          // Estilo Mundial (Grupos + Play-offs)
        CopaEliminacionDirecta         // Estilo Play-offs desde el inicio
    }

    public class Penca
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int DeporteId { get; set; }
        public Deporte? Deporte { get; set; }
        public int CantidadEquipos { get; set; }
        public ModoPenca Modo { get; set; }
        public bool Finalizada { get; set; } = false;

        // Relación con Equipos y Partidos
        public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
        public ICollection<Partido> Partidos { get; set; } = new List<Partido>();

        /// <summary>
        /// Colección de instanciaciones de esta penca en diferentes sitios de la plataforma.
        /// </summary>
        public ICollection<PencaInstancia> Instancias { get; set; } = new List<PencaInstancia>();
    }
}