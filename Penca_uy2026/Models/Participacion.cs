using Penca_uy2026.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Representa la inscripción de un usuario en una instancia de penca.
    /// Almacena el estado de pago, puntos acumulados y las predicciones realizadas.
    /// </summary>
    public class Participacion : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Indica si el usuario ha pagado el costo de entrada a esta penca.
        /// </summary>
        public bool EstaPagado { get; set; } = false;

        /// <summary>
        /// Puntos totales acumulados por el usuario en esta penca tras procesar resultados.
        /// </summary>
        [Required]
        public int PuntajeTotal { get; set; } = 0;

        // --- RELACIONES ---

        [Required]
        public int UsuarioSitioId { get; set; }
        
        [ForeignKey("UsuarioSitioId")]
        public UsuarioSitio UsuarioSitio { get; set; } = null!;

        [Required]
        public int PencaInstanciaId { get; set; }

        public int SitioId { get; set; }

        [ForeignKey("PencaInstanciaId")]
        public PencaInstancia PencaInstancia { get; set; } = null!;

        /// <summary>
        /// Predicciones realizadas por el usuario para los partidos de esta penca.
        /// </summary>
        public ICollection<Prediccion> Predicciones { get; set; } = new List<Prediccion>();

        /// <summary>
        /// Registro de transacciones de pago asociadas a esta participación.
        /// </summary>
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();

        /// <summary>
        /// Mensajes enviados por el usuario en el chat grupal de esta penca.
        /// </summary>
        public ICollection<MensajeChat> MensajesChat { get; set; } = new List<MensajeChat>();
    }
}
