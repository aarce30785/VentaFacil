using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services;
using VentaFacil.web.Services.Auth;

namespace VentaFacil.web.Controllers
{
    public class LoginController : Controller
    {
        private  readonly IAuthService _authService;
        private readonly IPasswordResetService _passwordResetService;

        public LoginController(IAuthService authService, IPasswordResetService passwordResetService)
        {
            _authService = authService;
            _passwordResetService = passwordResetService;
        }

        [HttpGet]
        public IActionResult OlvidoContrasena()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OlvidoContrasena(string correo)
        {
            if (string.IsNullOrEmpty(correo))
            {
                ModelState.AddModelError("", "Por favor ingrese su correo.");
                return View();
            }

            await _passwordResetService.RequestPasswordResetAsync(correo);
            
            // Siempre mostramos el mismo mensaje por seguridad
            TempData["SuccessMessage"] = "Si el correo existe, se han enviado las instrucciones.";
            return RedirectToAction("InicioSesion");
        }

        [HttpGet]
        public IActionResult RestablecerContrasena(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("InicioSesion");

            return View(new Models.Dto.ResetPasswordDto { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(Models.Dto.ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _passwordResetService.ResetPasswordAsync(model.Token, model.Contrasena);

            if (result)
            {
                TempData["SuccessMessage"] = "Contraseña restablecida exitosamente. Inicie sesión.";
                return RedirectToAction("InicioSesion");
            }

            ModelState.AddModelError("", "El token es inválido o ha expirado.");
            return View(model);
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
                    IsPersistent = loginDto.Recordarme,
                    ExpiresUtc = loginDto.Recordarme ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(30)
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
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
            
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
