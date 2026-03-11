using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace VentaFacil.web.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, VentaFacil.web.Data.ApplicationDbContext dbContext)
        {
            // Validar solo si el usuario está autenticado
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var usuarioIdClaim = context.User.FindFirst("UsuarioId")?.Value 
                                     ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(usuarioIdClaim, out int loggedUserId))
                {
                    var sessionTokenClaim = context.User.FindFirst("SessionToken")?.Value;
                    var usuario = await dbContext.Usuario.FindAsync(loggedUserId);
                    
                    if (usuario != null && !string.IsNullOrEmpty(usuario.SessionToken))
                    {
                        if (usuario.SessionToken != sessionTokenClaim)
                        {
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            context.Session.Clear();
                            context.Response.Redirect("/Login/InicioSesion");
                            return;
                        }
                    }
                }

                // Intentar obtener el ID de usuario de la sesión
                var usuarioId = context.Session.GetInt32("UsuarioId");
                
                // CONDICIÓN CRÍTICA:
                // Si el usuario tiene Cookie de Auth (IsAuthenticated = true)
                // PERO la sesión está vacía (usuarioId = null)
                // Significa que la memoria se borró (reinicio de servidor) pero la cookie persistió.
                if (!usuarioId.HasValue)
                {
                    // 1. Cerrar la sesión de autenticación (borrar cookie)
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    
                    // 2. Limpiar cualquier dato residual de sesión
                    context.Session.Clear();
                    
                    // 3. Forzar redirección al login
                    context.Response.Redirect("/Login/InicioSesion");
                    return; // Interrumpir el pipeline aquí
                }
            }

            await _next(context);
        }
    }
}
