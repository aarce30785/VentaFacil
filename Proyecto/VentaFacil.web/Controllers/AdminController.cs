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
        public async Task<IActionResult> IndexUsuarios(int pagina = 1, int cantidadPorPagina = 10, int? usuarioId = null, string accion = null)
        {
            try
            {
                var usuarios = await _adminService.GetUsuariosPaginadosAsync(pagina, cantidadPorPagina);

                if (usuarios == null)
                {
                    usuarios = new UsuarioListResponse
                    {
                        Usuarios = new List<UsuarioResponse>(),
                        PaginaActual = 1,
                        TotalPaginas = 1,
                        TotalUsuarios = 0
                    };
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
                            usuarios.UsuarioSeleccionado = usuarioForm;
                        }
                    }
                    else if (accion == "crear")
                    {
                        usuarios.UsuarioSeleccionado = new UsuarioFormDto
                        {
                            Estado = true
                        };
                    }
                }

                // CARGAR ROLES - CORREGIDO
                var roles = await _usuarioService.GetRolesAsync();
                ViewBag.Roles = roles ?? new List<SelectListItem>(); // ← Asegurar que no sea null

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
                    TotalUsuarios = 0
                };

                ViewBag.Roles = new List<SelectListItem>(); // ← Lista vacía en caso de error
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

        [HttpPost]
        public async Task<IActionResult> EditarUsuario([FromBody] UsuarioDto model)
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

                // Validar contraseñas si se están actualizando
                if (!string.IsNullOrEmpty(model.Contrasena) && model.Contrasena != model.ConfirmarContrasena)
                {
                    return Json(new { success = false, message = "Las contraseñas no coinciden" });
                }

                // Actualizar usuario
                var resultado = await _adminService.ActualizarUsuarioAsync(model);

                if (resultado)
                {
                    return Json(new { success = true, message = "Usuario actualizado correctamente" });
                }
                else
                {
                    return Json(new { success = false, message = "Error al actualizar el usuario" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

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
