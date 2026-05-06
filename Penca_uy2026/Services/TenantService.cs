using Microsoft.AspNetCore.Http;
using Penca_uy2026.Data;

namespace Penca_uy2026.Services
{
    public interface ITenantService
    {
        int? GetTenantId();
    }

    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public TenantService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public int? GetTenantId()
        {
            var host = _httpContextAccessor.HttpContext?.Request.Host.Value.ToLower();

            // Usamos un Scope para obtener el contexto y evitar problemas de dependencia circular
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

            // Asumiendo que agregaste la propiedad 'Url' a tu clase Sitio
            return dbContext.Sitios
                .Where(s => s.Url == host)
                .Select(s => s.Id)
                .FirstOrDefault();
        }
    }
}