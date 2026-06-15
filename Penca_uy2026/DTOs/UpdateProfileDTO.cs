namespace Penca_uy2026.DTOs
{
    public class UpdateProfileDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Slug { get; set; } = string.Empty;
    }
}
