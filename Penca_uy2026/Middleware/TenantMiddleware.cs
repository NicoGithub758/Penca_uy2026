using Penca_uy2026.Interfaces;
using Penca_uy2026.Services;

namespace Penca_uy2026.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
        {
            // El TenantService se encargará de extraer la URL y buscar el ID
            // Este método se ejecuta en cada petición antes de llegar al controlador
            await tenantService.SetTenantFromHostAsync();

            await _next(context);
        }
    }
}