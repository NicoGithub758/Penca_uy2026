using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Penca_uy2026.Filters
{
    public class AdminSitioAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Obtenemos el nombre del controlador y la acción
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            // Si ya estamos en el Login, NO aplicar el filtro para evitar el bucle
            if (controller == "AdminSitioAuth" && action == "Login")
            {
                return;
            }

            // Si no estamos en Login, verificamos la cookie
            var cookie = context.HttpContext.Request.Cookies["SitioId_Admin"];
            if (string.IsNullOrEmpty(cookie))
            {
                context.Result = new RedirectToActionResult("Login", "AdminSitioAuth", null);
            }

            base.OnActionExecuting(context);
        }
    }
}