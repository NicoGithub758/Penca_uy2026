using Penca_uy2026.Interfaces;

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
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // 1. DEFINIR RUTAS PÚBLICAS
            // Aquí agregas cualquier ruta que NO deba pasar por validaciones de sesión/login
            bool esRutaPublica = path.StartsWith("/account/") ||
                                 path.StartsWith("/adminauth/login") ||
                                 path.StartsWith("/home/");

            if (esRutaPublica)
            {
                // Si es pública, solo dejamos que el tenant se configure (por si acaso)
                // y pasamos al siguiente componente sin validar nada más.
                await tenantService.SetTenantFromHostAsync();
                await _next(context);
                return;
            }

            // 2. LÓGICA NORMAL PARA RUTAS PRIVADAS
            // Si no es pública, ejecutamos la lógica de validación de tenant y usuario
            await tenantService.SetTenantFromHostAsync();

            // Aquí es donde normalmente iría tu validación de usuario logueado
            // if (!UsuarioLogueado) { ... }

            await _next(context);
        }
    }
}