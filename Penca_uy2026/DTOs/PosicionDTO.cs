namespace Penca_uy2026.DTOs
{
    public class PosicionDTO
    {
        public int Posicion { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public int Puntos { get; set; }
        public bool EsUsuarioActual { get; set; }
    }

    public class MiPosicionDTO
    {
        public int Posicion { get; set; }
        public int Puntos { get; set; }
        public int TotalParticipantes { get; set; }
    }
}
