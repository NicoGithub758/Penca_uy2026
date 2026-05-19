namespace Penca_uy2026.DTOs
{
    public class PrediccionDTO
    {
        public int Id { get; set; }
        public int GolesEquipoLocal { get; set; }
        public int GolesEquipoVisitante { get; set; }
        public int ParticipacionId { get; set;} 
        public int PartidoId { get; set; }
        public int SitioId { get; set; }
    }
}