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
    public class UsuarioController : Controller
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
                    TempData["ErrorMessage"] = "No se pudo identificar el usuario - Sesión no encontrada";
                    return RedirectToAction("InicioSesion", "Login");
                }

                var perfil = await _usuarioService.PerfilUsuario(usuarioId.Value);

                if (perfil == null)
                {
                    TempData["ErrorMessage"] = "Error al cargar el perfil - Usuario no encontrado";
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
                TempData["ErrorMessage"] = "Ocurrió un error al cargar su perfil. Por favor, intente más tarde.";
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

                
                if (string.IsNullOrWhiteSpace(viewModel.Edicion.Nombre) &&
                    string.IsNullOrWhiteSpace(viewModel.Edicion.Correo) &&
                    string.IsNullOrWhiteSpace(viewModel.Edicion.NuevaContrasena))
                {
                    TempData["ErrorMessage"] = "Debe proporcionar al menos un campo para actualizar (Nombre, Correo o Contraseña)";

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

                        TempData["SuccessMessage"] = "Perfil actualizado correctamente";
                        return RedirectToAction("Perfil");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Error al actualizar perfil";
                    }
                }
                else
                {
                    
                    var errores = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, Errores = x.Value.Errors.Select(e => e.ErrorMessage) })
                        .ToList();

                    TempData["ErrorMessage"] = "Por favor, corrige los errores del formulario";
                }

                await RecargarDatosUsuario(usuarioId, viewModel);
                return View("Perfil", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
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
    }
}
