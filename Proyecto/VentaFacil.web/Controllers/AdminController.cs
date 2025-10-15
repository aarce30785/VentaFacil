using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Admin;
using VentaFacil.web.Models.Response.Usuario;
using VentaFacil.web.Services.Admin;
using VentaFacil.web.Services.Usuario;

namespace VentaFacil.web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUsuarioService _usuarioService;

        public AdminController(IAdminService adminService, IUsuarioService usuarioService)
        {
            _adminService = adminService;
            _usuarioService = usuarioService;
        }

        public async Task<IActionResult> Index()
        {
            
            return await IndexUsuarios();
        }

        // En tu AdminController
        public async Task<IActionResult> IndexUsuarios(int pagina = 1, int cantidadPorPagina = 10,
                     string busqueda = null, int? rolFiltro = null, int? usuarioId = null, string accion = null)
        {
            try
            {
                // Pasar los parámetros de filtro al servicio
                var usuarios = await _adminService.GetUsuariosPaginadosAsync(pagina, cantidadPorPagina, busqueda, rolFiltro);

                if (usuarios == null)
                {
                    usuarios = new UsuarioListResponse
                    {
                        Usuarios = new List<UsuarioResponse>(),
                        PaginaActual = 1,
                        TotalPaginas = 1,
                        TotalUsuarios = 0,
                        Busqueda = busqueda,
                        RolFiltro = rolFiltro
                    };
                }
                else
                {
                    // Asegurar que los filtros se mantengan en la respuesta
                    usuarios.Busqueda = busqueda;
                    usuarios.RolFiltro = rolFiltro;
                }

                // Manejar acciones del modal
                if (!string.IsNullOrEmpty(accion))
                {
                    usuarios.AccionModal = accion;
                    ViewBag.AccionModal = accion;

                    if (accion == "editar" || accion == "ver")
                    {
                        if (usuarioId.HasValue)
                        {
                            var usuarioForm = await _usuarioService.GetUsuarioByIdAsync(usuarioId.Value);
                            // Convertir UsuarioDto a UsuarioResponse
                            usuarios.UsuarioSeleccionado = new UsuarioResponse
                            {
                                Id_Usr = usuarioForm.Id_Usr,
                                Nombre = usuarioForm.Nombre,
                                Correo = usuarioForm.Correo,
                                Estado = usuarioForm.Estado,
                                RolId = usuarioForm.Rol,
                            };
                        }
                    }
                    else if (accion == "crear")
                    {
                        usuarios.UsuarioSeleccionado = new UsuarioResponse
                        {
                            Estado = true
                        };
                    }
                }

                // CARGAR ROLES
                var roles = await _usuarioService.GetRolesAsync();
                ViewBag.Roles = roles ?? new List<SelectListItem>();

                ViewBag.PaginaActual = pagina;

                return View("Index", usuarios);
            }
            catch (Exception ex)
            {
                var emptyResponse = new UsuarioListResponse
                {
                    Usuarios = new List<UsuarioResponse>(),
                    PaginaActual = 1,
                    TotalPaginas = 1,
                    TotalUsuarios = 0,
                    Busqueda = busqueda,
                    RolFiltro = rolFiltro
                };

                ViewBag.Roles = new List<SelectListItem>();
                return View("Index", emptyResponse);
            }
        }


        [HttpPost]
        public async Task<IActionResult> CrearUsuario([FromBody] UsuarioDto model)
        {
            try
            {
                // Validar modelo
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Datos inválidos", errors });
                }

                // Validar que las contraseñas coincidan
                if (model.Contrasena != model.ConfirmarContrasena)
                {
                    return Json(new { success = false, message = "Las contraseñas no coinciden" });
                }

                // Crear usuario
                var resultado = await _adminService.CrearUsuarioAsync(model);

                if (resultado)
                {
                    return Json(new { success = true, message = "Usuario creado correctamente" });
                }
                else
                {
                    return Json(new { success = false, message = "Error al crear el usuario" });
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LimpiarFiltros()
        {
            return RedirectToAction("IndexUsuarios", new
            {
                pagina = 1,
                cantidadPorPagina = 10,
                busqueda = (string)null,
                rolFiltro = (int?)null
            });
        }


        [HttpGet]
        public async Task<IActionResult> DetalleUsuario(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar los detalles del usuario";
                return RedirectToAction(nameof(IndexUsuarios));
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> EditarUsuario([FromBody] UsuarioDto model)
        //{
        //    try
        //    {
        //        // Validar modelo
        //        if (!ModelState.IsValid)
        //        {
        //            var errors = ModelState.Values
        //                .SelectMany(v => v.Errors)
        //                .Select(e => e.ErrorMessage)
        //                .ToList();
        //            return Json(new { success = false, message = "Datos inválidos", errors });
        //        }
        //
        //        // Validar contraseñas si se están actualizando
        //        if (!string.IsNullOrEmpty(model.Contrasena) && model.Contrasena != model.ConfirmarContrasena)
        //        {
        //            return Json(new { success = false, message = "Las contraseñas no coinciden" });
        //        }
        //
        //        // Actualizar usuario
        //        var resultado = await _adminService.ActualizarUsuarioAsync(model);
        //
        //        if (resultado)
        //        {
        //            return Json(new { success = true, message = "Usuario actualizado correctamente" });
        //        }
        //        else
        //        {
        //            return Json(new { success = false, message = "Error al actualizar el usuario" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            try
            {
                await _adminService.EliminarUsuarioAsync(id);

                if (Request.Headers["Content-Type"] == "application/json")
                {
                    return Json(new { success = true, message = "Usuario eliminado correctamente" });
                }

                TempData["Success"] = "Usuario eliminado correctamente";
                return RedirectToAction(nameof(IndexUsuarios));
            }
            catch (Exception ex)
            {
                if (Request.Headers["Content-Type"] == "application/json")
                {
                    return Json(new { success = false, message = ex.Message });
                }

                TempData["Error"] = "Error al eliminar el usuario";
                return RedirectToAction(nameof(IndexUsuarios));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetalleRol(int id)
        {
            try
            {
                var rol = await _usuarioService.GetRolByIdAsync(id);
                return View(rol);
            }
            catch (Exception ex)
            {
                // Log the exception
                TempData["Error"] = "Error al cargar los detalles del rol";
                return RedirectToAction(nameof(Index));
            }
        }

        

        

    }
}
