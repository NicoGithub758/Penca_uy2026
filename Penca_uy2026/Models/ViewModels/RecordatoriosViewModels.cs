using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models.ViewModels
{
    /// <summary>
    /// ViewModel para la configuracion de recordatorios automaticos del sitio.
    /// </summary>
    public class ConfiguracionSitioViewModel
    {
        [Display(Name = "Enviar recordatorios automáticos antes de cada partido")]
        public bool RecordatoriosAutomaticosActivos { get; set; } = true;

        [Required]
        [Range(1, 72, ErrorMessage = "El valor debe estar entre 1 y 72 horas")]
        [Display(Name = "Horas antes del partido")]
        public int HorasAntes { get; set; } = 1;
    }

    /// <summary>
    /// ViewModel para enviar un anuncio inmediato a los participantes de una penca.
    /// </summary>
    public class EnviarAnuncioViewModel
    {
        [Required(ErrorMessage = "Tenés que seleccionar una penca")]
        [Display(Name = "Penca destino")]
        public int PencaInstanciaId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [MaxLength(100)]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es obligatorio")]
        [MaxLength(500)]
        [Display(Name = "Mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        // Lista de pencas disponibles para el dropdown (se llena en el GET)
        public List<PencaOptionViewModel> PencasDisponibles { get; set; } = new();
    }

    public class PencaOptionViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
