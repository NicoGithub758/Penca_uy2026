namespace Penca_uy2026.Interfaces
{
    public interface ITenantService
    {
        int? GetTenantId();
        Task SetTenantFromHostAsync();
    }
}
