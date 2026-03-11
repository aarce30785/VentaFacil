using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services;
using VentaFacil.web.Services.Auth;

namespace VentaFacil.web.Controllers
{
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public class LoginController : BaseController
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
                SetAlert("Por favor ingrese su correo.", "warning");
                return RedirectToAction("InicioSesion");
            }

            await _passwordResetService.RequestPasswordResetAsync(correo);
            
            // Siempre mostramos el mismo mensaje por seguridad
            SetAlert("Si el correo existe, se han enviado las instrucciones.", "success", "Solicitud Enviada");
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
        public IActionResult ActivarCuenta(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("InicioSesion");

            // Reusamos el DTO de reset password ya que necesitamos los mismos campos
            return View(new Models.Dto.ResetPasswordDto { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivarCuenta(Models.Dto.ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Reusamos el servicio de reset password ya que la lógica es idéntica (buscar token, validar, actualizar pass)
            // La diferencia es que internamente el servicio ahora también activa el usuario.
            var result = await _passwordResetService.ResetPasswordAsync(model.Token, model.Contrasena);

            if (result)
            {
                SetAlert("¡Cuenta activada correctamente! Ya puedes iniciar sesión.", "success", "Bienvenido");
                return RedirectToAction("InicioSesion");
            }

            ModelState.AddModelError("", "El enlace de activación es inválido o ha expirado.");
            return View(model);
        }

        [HttpGet]
        public IActionResult InicioSesion()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            
            
            var model = new LoginDto();
            if (TempData["SavedCorreo"] != null)
            {
                model.Correo = TempData["SavedCorreo"].ToString();
            }
            return View(model);
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

                // TempData["SuccessMessage"] = result.Message; // Removed in favor of SetAlert
                SetAlert($"Bienvenido nuevamente, {result.Nombre}", "success", "Inicio de Sesión Correcto");

                // **CORREGIR: Redirigir explícitamente al Home/Index**
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home"); // Esta línea está bien
            }
            else
            {
                SetAlert(result.Message, "danger", "Error de Acceso");
                // Save email to avoid retyping
                TempData["SavedCorreo"] = loginDto.Correo;
                return RedirectToAction("InicioSesion");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
            
            HttpContext.Session.Clear();
        
            SetAlert("Sesión cerrada correctamente", "info", "Hasta pronto");
            return RedirectToAction("InicioSesion", "Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
