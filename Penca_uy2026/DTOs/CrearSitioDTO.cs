namespace Penca_uy2026.DTOs
{
    public class CrearSitioDTO
    {
        public string NombreSitio { get; set; }
        public string UrlVercel { get; set; } // Ej: empresa-x.vercel.app
        public string EmailAdmin { get; set; }
        public string PasswordAdmin { get; set; }
        public string NombreAdmin { get; set; }
    }
}