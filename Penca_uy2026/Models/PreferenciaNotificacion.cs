using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Penca_uy2026.Interfaces;

namespace Penca_uy2026.Models
{
    /// <summary>
    /// Preferencias de notificaciones push de un usuario en un sitio.
    /// Cada UsuarioSitio tiene UNA fila de preferencias (relacion 1-1).
    /// Al registrar un usuario, se crean las preferencias con todos los tipos en true.
    /// </summary>
    public class PreferenciaNotificacion : IMultiTenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioSitioId { get; set; }

        [ForeignKey("UsuarioSitioId")]
        public UsuarioSitio UsuarioSitio { get; set; } = null!;

        public int SitioId { get; set; }

        // ----- TIPOS DE NOTIFICACIONES (todos default true) -----

        /// <summary>
        /// Cuando un admin confirma el resultado de un partido en una penca donde participo.
        /// </summary>
        public bool RecibirResultados { get; set; } = true;

        /// <summary>
        /// Recordatorios antes de que cierre la prediccion de un partido.
        /// </summary>
        public bool RecibirPartidos { get; set; } = true;

        /// <summary>
        /// Anuncios y mensajes generales del administrador del sitio.
        /// </summary>
        public bool RecibirGenerales { get; set; } = true;

        /// <summary>
        /// Cambios en mi posicion en la tabla (alguien me supero, sub i puestos, etc).
        /// </summary>
        public bool RecibirRanking { get; set; } = true;
    }
}
