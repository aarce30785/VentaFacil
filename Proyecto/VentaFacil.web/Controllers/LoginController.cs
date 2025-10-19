using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services;

namespace VentaFacil.web.Controllers
{
    public class LoginController : Controller
    {
        private  readonly IAuthService _authService;

        public LoginController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult InicioSesion()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InicioSesion(LoginDto loginDto, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(loginDto);
            }

            var result = await _authService.LoginAsync(loginDto);

            if (result.Success)
            {
                // Crear claims para el usuario
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.UsuarioId.ToString()),
                    new Claim(ClaimTypes.Name, result.Nombre),
                    new Claim(ClaimTypes.Email, loginDto.Correo),
                    new Claim(ClaimTypes.Role, result.Rol),
                    new Claim("UsuarioId", result.UsuarioId.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                HttpContext.Session.SetString("UsuarioNombre", result.Nombre);
                HttpContext.Session.SetString("UsuarioRol", result.Rol);
                HttpContext.Session.SetInt32("UsuarioId", result.UsuarioId);
                HttpContext.Session.SetString("IsLoggedIn", "true");

                TempData["SuccessMessage"] = result.Message;

                // **CORREGIR: Redirigir explícitamente al Home/Index**
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home"); // Esta línea está bien
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(loginDto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Cerrar sesión de cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
            // Limpiar sesión
            HttpContext.Session.Clear();
        
            TempData["SuccessMessage"] = "Sesión cerrada correctamente";
            return RedirectToAction("InicioSesion", "Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
