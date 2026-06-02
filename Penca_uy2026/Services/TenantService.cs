using Penca_uy2026.Data;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Interfaces;

namespace Penca_uy2026.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TenantService> _logger;
        private int? _currentSitioId;

        public TenantService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider, ILogger<TenantService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public int? GetTenantId() => _currentSitioId;

        public async Task SetTenantFromHostAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            // LOG TEMPORAL
            var claims = httpContext.User?.Claims?.ToList();
            _logger.LogInformation($"[TENANT] Cantidad de claims: {claims?.Count ?? 0}");
            if (claims != null)
            {
                foreach (var claim in claims)
                {
                    _logger.LogInformation($"[TENANT] Claim: {claim.Type} = {claim.Value}");
                }
            }

            // PRIORIDAD 1: leer el claim "sitioId" del JWT (mobile y web autenticado)
            var sitioIdClaim = httpContext.User?.FindFirst("sitioId")?.Value;
            if (!string.IsNullOrEmpty(sitioIdClaim) && int.TryParse(sitioIdClaim, out int sitioIdJwt))
            {
                _currentSitioId = sitioIdJwt;
                return;
            }

            // PRIORIDAD 2: leer el slug del path de la URL (frontend web)
            var path = httpContext.Request.Path.Value;
            if (!string.IsNullOrEmpty(path))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    var slug = segments[0];

                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

                    var sitio = await dbContext.Sitios
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(s => s.Slug == slug);

                    _currentSitioId = sitio?.Id;
                }
            }
        }
    }
}