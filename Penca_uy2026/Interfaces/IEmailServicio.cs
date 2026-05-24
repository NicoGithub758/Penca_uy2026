namespace Penca_uy2026.Interfaces
{
    public interface IEmailServicio
    {
        Task EnviarEmailInvitacionAsync(string emailDestino, string nombreAdmin, string tokenInvitacion, string urlSitio);
    }
}