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


        [HttpGet]
        public async Task<IActionResult> ObtenerModalUsuario(string accion, int? usuarioId = null,
            string busqueda = null, int? rolFiltro = null, int pagina = 1)
        {
            try
            {
                var model = new UsuarioDto();
                ViewBag.AccionModal = accion;

                // Mantener los filtros para el retorno
                ViewBag.BusquedaActual = busqueda;
                ViewBag.RolFiltroActual = rolFiltro;
                ViewBag.PaginaActual = pagina;

                // Cargar roles
                var roles = await _usuarioService.GetRolesAsync();
                ViewBag.Roles = roles ?? new List<SelectListItem>();

                if (usuarioId.HasValue && (accion == "editar" || accion == "ver"))
                {
                    var usuario = await _usuarioService.GetUsuarioByIdAsync(usuarioId.Value);
                    if (usuario != null)
                    {
                        model = usuario;
                    }
                }
                else if (accion == "crear")
                {
                    model.Estado = true; // Por defecto activo
                }

                return PartialView("_UsuarioModal", model);
            }
            catch (Exception ex)
            {
                // En caso de error, devolver un modal vacío
                ViewBag.AccionModal = accion;
                ViewBag.Roles = new List<SelectListItem>();
                return PartialView("_UsuarioModal", new UsuarioDto());
            }
        }
        [HttpPost]
        public async Task<IActionResult> GuardarUsuario([FromBody] UsuarioDto model)
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

                // Validar que las contraseñas coincidan (solo para crear o cuando se cambia la contraseña)
                if ((model.Id_Usr == 0 || !string.IsNullOrEmpty(model.Contrasena)) &&
                    model.Contrasena != model.ConfirmarContrasena)
                {
                    return Json(new { success = false, message = "Las contraseñas no coinciden" });
                }

                bool resultado;

                if (model.Id_Usr == 0)
                {
                    // Crear usuario
                    resultado = await _adminService.CrearUsuarioAsync(model);
                }
                else
                {
                    // Actualizar usuario
                    resultado = await _adminService.ActualizarUsuarioAsync(model);
                }

                if (resultado)
                {
                    var action = model.Id_Usr == 0 ? "creado" : "actualizado";
                    return Json(new
                    {
                        success = true,
                        message = $"Usuario {action} correctamente"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Error al guardar el usuario" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> EliminarUsuario(int id, string? busqueda, int? rolFiltro, int pagina = 1)
        {
            try
            {
                bool resultado = await _adminService.EliminarUsuarioAsync(id);

                if (resultado)
                    TempData["MensajeExito"] = "Usuario eliminado correctamente.";
                else
                    TempData["MensajeError"] = "No se pudo eliminar el usuario.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al eliminar el usuario: {ex.Message}";
            }

            // Redirigir manteniendo los filtros
            return RedirectToAction("IndexUsuarios", new { busqueda, rolFiltro, pagina });
        }














    }
}
