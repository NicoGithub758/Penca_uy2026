using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class InvitacionAdmin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public int UsuarioSitioId { get; set; }

        [ForeignKey("UsuarioSitioId")]
        public UsuarioSitio UsuarioSitio { get; set; } = null!;

        [Required]
        public DateTime FechaExpiracion { get; set; }

        public bool Usado { get; set; } = false;

        // Propiedad calculada para saber si el token sigue vigente
        public bool IsValido => !Usado && DateTime.UtcNow < FechaExpiracion;
    }
}