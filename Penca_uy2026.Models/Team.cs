using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Penca_uy2026.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe indicar un nombre")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // --- Relaciones ---

        // Una relación como equpio local y otra como visitante.

        // Esta anotación le dice a EF Core con qué propiedad específicamente corresponde "del otro lado" de las relaciones con Team.
        [InverseProperty("HomeTeam")]
        public ICollection<Event> HomeEvents { get; set; } = new List<Event>();

        [InverseProperty("AwayTeam")]
        public ICollection<Event> AwayEvents { get; set; } = new List<Event>();
    }
}
