using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
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
                     string? busqueda = null, int? rolFiltro = null, int? usuarioId = null, string? accion = null)
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
            catch
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
            string? busqueda = null, int? rolFiltro = null, int pagina = 1)
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
            catch
            {
                // En caso de error, devolver un modal vacío
                ViewBag.AccionModal = accion;
                ViewBag.Roles = new List<SelectListItem>();
                return PartialView("_UsuarioModal", new UsuarioDto());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarUsuario(
            [FromForm] UsuarioDto usuarioDto,
            [FromForm] string busqueda = null,
            [FromForm] int? rolFiltro = null,
            [FromForm] int pagina = 1)
        {
            try
            {
                // DEBUG: Log para verificar que los datos llegan
                Console.WriteLine($"Datos recibidos - ID: {usuarioDto.Id_Usr}, Nombre: {usuarioDto.Nombre}, Correo: {usuarioDto.Correo}, Rol: {usuarioDto.Rol}");
                Console.WriteLine($"Contraseña recibida: {(string.IsNullOrEmpty(usuarioDto.Contrasena) ? "VACÍA" : "PRESENTE")}");
                Console.WriteLine($"Filtros - Búsqueda: {busqueda}, RolFiltro: {rolFiltro}, Página: {pagina}");

                // Limpiar ModelState antes de validar
                ModelState.Clear();

                // Remover errores de validación de contraseña para edición si están vacías
                if (usuarioDto.Id_Usr > 0 && string.IsNullOrEmpty(usuarioDto.Contrasena))
                {
                    // Para edición, si la contraseña está vacía, ignorar validación de contraseñas
                    usuarioDto.Contrasena = null;
                    usuarioDto.ConfirmarContrasena = null;

                    // Remover del ModelState explícitamente
                    ModelState.Remove("Contrasena");
                    ModelState.Remove("ConfirmarContrasena");
                }

                // Validar manualmente usando IValidatableObject
                var validationContext = new ValidationContext(usuarioDto, null, null);
                var validationResults = new List<ValidationResult>();
                bool isValid = Validator.TryValidateObject(usuarioDto, validationContext, validationResults, true);

                // Agregar errores de validación personalizados al ModelState
                foreach (var validationResult in validationResults)
                {
                    foreach (var memberName in validationResult.MemberNames)
                    {
                        if (!ModelState.ContainsKey(memberName) || !ModelState[memberName].Errors.Any(e => e.ErrorMessage == validationResult.ErrorMessage))
                        {
                            ModelState.AddModelError(memberName, validationResult.ErrorMessage);
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(ms => ms.Value.Errors.Any())
                        .SelectMany(ms => ms.Value.Errors
                            .Where(e => !string.IsNullOrEmpty(e.ErrorMessage))
                            .Select(e => new { Field = ms.Key, Message = e.ErrorMessage }))
                        .ToList();

                    var errorMessages = errors.Select(e => e.Message).Distinct().ToList();
                    var fieldErrors = errors.ToDictionary(e => e.Field, e => e.Message);

                    // Log para debugging
                    Console.WriteLine($"Errores de validación: {string.Join(", ", errorMessages)}");

                    // RETORNAR JSON explícitamente
                    return BadRequest(new // Usar BadRequest para status 400
                    {
                        success = false,
                        message = "Error de validación",
                        errors = errorMessages,
                        fieldErrors = fieldErrors
                    });
                }

                bool result;

                if (usuarioDto.Id_Usr > 0)
                {
                    // Actualizar usuario existente
                    result = await _adminService.ActualizarUsuarioAsync(usuarioDto);
                }
                else
                {
                    // Crear nuevo usuario - necesitas un método diferente
                    result = await _adminService.CrearUsuarioAsync(usuarioDto);
                }

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = usuarioDto.Id_Usr > 0 ? "Usuario actualizado correctamente" : "Usuario creado correctamente"
                    });
                }
                else
                {
                    // AÑADIR ESTE RETURN FALTANTE
                    var operation = usuarioDto.Id_Usr > 0 ? "actualizar" : "crear";
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Error al {operation} el usuario",
                        errors = new List<string> { $"No se pudo {operation} el usuario en la base de datos" }
                    });
                }
            }
            catch (Exception ex)
            {
                // Log de la excepción completa
                Console.WriteLine($"Excepción en GuardarUsuario: {ex}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    errors = new List<string> { ex.Message }
                });
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
