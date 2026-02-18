using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.ViewModel;
using VentaFacil.web.Services.Usuario;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class UsuarioController : BaseController
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usurarioService)
        {
            _usuarioService = usurarioService;
        }

        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            try
            {
                var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

                if (usuarioId == null)
                {
                    SetAlert("No se pudo identificar el usuario - Sesión no encontrada", "error");
                    return RedirectToAction("InicioSesion", "Login");
                }

                var perfil = await _usuarioService.PerfilUsuario(usuarioId.Value);

                if (perfil == null)
                {
                    SetAlert("Error al cargar el perfil - Usuario no encontrado", "error");
                    return View("ErrorPerfil");
                }

                HttpContext.Session.SetString("UsuarioNombre", perfil.Nombre);
                HttpContext.Session.SetString("UsuarioCorreo", perfil.Correo);
                HttpContext.Session.SetString("UsuarioRol", perfil.Rol ?? "Usuario");

                var viewModel = new PerfilViewModel
                {
                    Usuario = perfil,
                    Edicion = new UsuarioPerfilDto
                    {
                        Id_Usr = perfil.Id_Usr,
                        Nombre = perfil.Nombre,
                        Correo = perfil.Correo,
                        
                    }
                };

                return View("Perfil", viewModel);
            }
            catch (Exception ex)
            {
                SetAlert("Ocurrió un error al cargar su perfil. Por favor, intente más tarde.", "error");
                return View("ErrorPerfil");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPerfil(PerfilViewModel viewModel)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            try
            {
                
                foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Usuario.")).ToList())
                {
                    ModelState.Remove(key);
                }

                ModelState.Remove("Usuario");

                if (string.IsNullOrEmpty(viewModel.Edicion.Nombre))
                {
                    ModelState.Remove("Edicion.Nombre");
                }

                if (string.IsNullOrEmpty(viewModel.Edicion.Correo))
                {
                    ModelState.Remove("Edicion.Correo");
                }

                if (string.IsNullOrEmpty(viewModel.Edicion.NuevaContrasena))
                {
                    ModelState.Remove("Edicion.ContrasenaActual");
                    ModelState.Remove("Edicion.NuevaContrasena");
                    ModelState.Remove("Edicion.ConfirmarContrasena");
                }
                else
                {
                    // Validar política de contraseñas
                    var errorContrasena = ValidarPoliticaContrasena(viewModel.Edicion.NuevaContrasena);
                    if (errorContrasena != null)
                    {
                        ModelState.AddModelError("Edicion.NuevaContrasena", errorContrasena);
                    }

                    if (viewModel.Edicion.NuevaContrasena == viewModel.Edicion.ContrasenaActual)
                    {
                         ModelState.AddModelError("Edicion.NuevaContrasena", "La nueva contraseña no puede ser igual a la actual.");
                    }
                }

                
                if (string.IsNullOrWhiteSpace(viewModel.Edicion.Nombre) &&
                    string.IsNullOrWhiteSpace(viewModel.Edicion.Correo) &&
                    string.IsNullOrWhiteSpace(viewModel.Edicion.NuevaContrasena))
                {
                    SetAlert("Debe proporcionar al menos un campo para actualizar (Nombre, Correo o Contraseña)", "warning");

                    await RecargarDatosUsuario(usuarioId, viewModel);
                    return View("Perfil", viewModel);
                }

                if (ModelState.IsValid)
                {
                    var resultado = await _usuarioService.ActualizarPerfilAsync(viewModel.Edicion);

                    if (resultado)
                    {
                        
                        var usuarioActualizado = await _usuarioService.GetUsuarioByIdAsync(viewModel.Edicion.Id_Usr);

                        
                        if (usuarioActualizado != null)
                        {
                            
                            if (!string.IsNullOrWhiteSpace(viewModel.Edicion.Nombre))
                            {
                                HttpContext.Session.SetString("UsuarioNombre", usuarioActualizado.Nombre);
                            }

                            if (!string.IsNullOrWhiteSpace(viewModel.Edicion.Correo))
                            {
                                HttpContext.Session.SetString("UsuarioCorreo", usuarioActualizado.Correo);
                            }

                            if (usuarioActualizado.Rol != null) 
                            {
                                HttpContext.Session.SetString("UsuarioRol", usuarioActualizado.Rol.ToString());
                            }
                        }

                        SetAlert("Perfil actualizado correctamente", "success");
                        return RedirectToAction("Perfil");
                    }
                    else
                    {
                        SetAlert("Error al actualizar perfil", "error");
                    }
                }
                else
                {
                    
                    var errores = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, Errores = x.Value.Errors.Select(e => e.ErrorMessage) })
                        .ToList();

                     // SetAlert("Por favor, corrige los errores del formulario", "error");
                }

                await RecargarDatosUsuario(usuarioId, viewModel);
                return View("Perfil", viewModel);
            }
            catch (Exception ex)
            {
                SetAlert(ex.Message, "error");
                await RecargarDatosUsuario(usuarioId, viewModel);
                return View("Perfil", viewModel);
            }
        }

        private async Task RecargarDatosUsuario(int? usuarioId, PerfilViewModel viewModel)
        {
            if (usuarioId != null)
            {
                var perfil = await _usuarioService.PerfilUsuario(usuarioId.Value);
                viewModel.Usuario = perfil;
            }
        }

        private string ValidarPoliticaContrasena(string contrasena)
        {
            if (string.IsNullOrEmpty(contrasena)) return "La contraseña no puede estar vacía.";
            
            if (contrasena.Length < 12) return "La contraseña debe tener al menos 12 caracteres.";
            
            if (!contrasena.Any(char.IsUpper)) return "La contraseña debe contener al menos una letra mayúscula (A-Z).";
            
            if (!contrasena.Any(char.IsLower)) return "La contraseña debe contener al menos una letra minúscula (a-z).";
            
            if (!contrasena.Any(char.IsDigit)) return "La contraseña debe contener al menos un número (0-9).";
            
            if (!contrasena.Any(ch => !char.IsLetterOrDigit(ch))) return "La contraseña debe contener al menos un carácter especial.";
            
            return null;
        }
    }
}
