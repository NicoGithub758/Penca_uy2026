namespace Penca_uy2026.DTOs
{
    public class GenerarInvitacionRequest
    {
        public string Slug { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class GenerarInvitacionResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
