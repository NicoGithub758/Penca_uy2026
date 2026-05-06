using Penca_uy2026.Data;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Interfaces;


namespace Penca_uy2026.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private int? _currentSitioId;

        public TenantService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public int? GetTenantId() => _currentSitioId;

        public async Task SetTenantFromHostAsync()
        {
            var host = _httpContextAccessor.HttpContext?.Request.Host.Value;

            if (!string.IsNullOrEmpty(host))
            {
                // Usamos un Scope temporal para consultar la base de datos sin ciclos infinitos
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

                // Buscamos el sitio que coincida con la URL actual
                var sitio = await dbContext.Sitios
                    .IgnoreQueryFilters() // ¡IMPORTANTE! Para poder encontrar el sitio
                    .FirstOrDefaultAsync(s => s.Url == host);

                _currentSitioId = sitio?.Id;
            }
        }
    }
}