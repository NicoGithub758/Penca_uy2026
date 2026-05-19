using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Penca_uy2026.Interfaces;

namespace Penca_uy2026.Models
{
    public class Partido
    {
        
        [Key]
        public int Id { get; set; }
        public int? GolesLocal { get; set; }
        public int? GolesVisitante { get; set; }
        public DateTime Fecha { get; set; }
        public string Fase { get; set; } = "Regular"; // Ej: "Octavos", "Cuartos", "Final"
        public bool Jugado { get; set; } = false;

       // -----------------  RELACIONES  -------------------
                
        [Column("Local")]
        public int idLocal { get; set; }
        [ForeignKey("idLocal")]
        public Equipo Local { get; set; } = null!;

        [Column("Visitante")]
        public int idVisitante { get; set; }

        [ForeignKey("idVisitante")]
        public Equipo Visitante { get; set; } = null!;
        [Required]
        public int PencaId { get; set; }    
        [ForeignKey("PencaId")]
        public Penca Penca { get; set; } = null!;        

    
    }
    
}
