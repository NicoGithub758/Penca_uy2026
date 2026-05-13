using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models.ViewModels
{
    public class CrearSitioViewModel
    {
        public string NombreSitio { get; set; }
        public string UrlVercel { get; set; }
        public string NombreAdmin { get; set; }
        public string EmailAdmin { get; set; }
        public string PasswordAdmin { get; set; }
        public TipoRegistro TipoRegistro { get; set; }
    }
}