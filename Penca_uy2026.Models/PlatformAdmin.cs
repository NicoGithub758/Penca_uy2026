using System.ComponentModel.DataAnnotations;

namespace Penca_uy2026.Models
{
    public class PlatformAdmin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
    }
}