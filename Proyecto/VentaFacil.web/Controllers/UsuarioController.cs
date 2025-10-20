using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentaFacil.web.Services.Usuario;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioService _usurarioService;

        public UsuarioController(IUsuarioService usurarioService)
        {
            _usurarioService = usurarioService;
        }

        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            try
            {
                var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

                // DEBUG: Verificar sesión
                Console.WriteLine($"UsuarioId desde sesión: {usuarioId}");

                if (usuarioId == null)
                {
                    TempData["ErrorMessage"] = "No se pudo identificar el usuario - Sesión no encontrada";
                    Console.WriteLine("Redirigiendo a Login - Sesión inválida");
                    return RedirectToAction("InicioSesion", "Login");
                }

                var perfil = await _usurarioService.PerfilUsuario(usuarioId.Value);

                // DEBUG: Verificar perfil
                Console.WriteLine($"Perfil obtenido: {(perfil != null ? "OK" : "NULL")}");

                if (perfil == null)
                {
                    TempData["ErrorMessage"] = "Error al cargar el perfil - Usuario no encontrado";
                    Console.WriteLine("Mostrando ErrorPerfil - Perfil nulo");
                    return View("ErrorPerfil");
                }

                Console.WriteLine("Mostrando vista Perfil con datos");
                return View("Perfil", perfil);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción en Perfil: {ex.Message}");
                TempData["ErrorMessage"] = "Ocurrió un error al cargar su perfil. Por favor, intente más tarde.";
                return View("ErrorPerfil");
            }
        }
    }
}
