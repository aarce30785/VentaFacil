using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Admin;
using VentaFacil.web.Models.Response.Producto;
using VentaFacil.web.Models.Response.Usuario;
using VentaFacil.web.Services.Admin;
using VentaFacil.web.Services.Producto;
using VentaFacil.web.Services.Usuario;

namespace VentaFacil.web.Controllers
{
    [Authorize(Roles="Administrador")]
    public class AdminController : BaseController
    {
        private readonly IAdminService _adminService;
        private readonly IUsuarioService _usuarioService;
        private readonly IProductoService _productoService;
        private readonly ICategoriaService _categoriaService;

        public AdminController(
            IAdminService adminService,
            IUsuarioService usuarioService,
            IProductoService productoService,
            ICategoriaService categoriaService)
        {
            _adminService = adminService;
            _usuarioService = usuarioService;
            _productoService = productoService;
            _categoriaService = categoriaService;
        }

        public async Task<IActionResult> Index()
        {
            return await IndexUsuarios();
        }

        [HttpGet]
        public IActionResult LimpiarFiltros()
        {
            return RedirectToAction("IndexUsuarios");
        }

        [HttpGet]
        public IActionResult LimpiarFiltrosProductos()
        {
            return RedirectToAction("Index");
        }

        // ========== MÉTODOS PARA USUARIOS ==========
        public async Task<IActionResult> IndexUsuarios(int pagina = 1, int cantidadPorPagina = 10,
                     string? busqueda = null, int? rolFiltro = null, bool mostrarInactivos = false, int? usuarioId = null, string? accion = null)
        {
            try
            {
                ViewBag.MostrarInactivos = mostrarInactivos;
                // Pasar los parámetros de filtro al servicio
                var usuarios = await _adminService.GetUsuariosPaginadosAsync(pagina, cantidadPorPagina, busqueda, rolFiltro, mostrarInactivos);

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

                // Cargar también los productos para la pestaña de productos
                var productosResponse = await GetProductosResponse();
                ViewData["Productos"] = productosResponse;

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

                // Cargar productos incluso en caso de error
                var productosResponse = await GetProductosResponse();
                ViewData["Productos"] = productosResponse;

                return View("Index", emptyResponse);
            }
        }

        // ========== MÉTODOS PARA PRODUCTOS ==========
        [HttpGet]
        public async Task<IActionResult> IndexProductos(int pagina = 1, int cantidadPorPagina = 10,
                     string? busqueda = null, int? categoriaFiltro = null, int? productoId = null, string? accion = null)
        {
            try
            {
                // Primero cargar los usuarios (modelo principal de la vista)
                var usuariosResponse = await _adminService.GetUsuariosPaginadosAsync(1, 10, null, null);
                if (usuariosResponse == null)
                {
                    usuariosResponse = new UsuarioListResponse
                    {
                        Usuarios = new List<UsuarioResponse>(),
                        PaginaActual = 1,
                        TotalPaginas = 1,
                        TotalUsuarios = 0
                    };
                }

                // Cargar productos con filtros
                var productosResponse = await GetProductosResponse(pagina, cantidadPorPagina, busqueda, categoriaFiltro);
                ViewData["Productos"] = productosResponse;

                // Cargar roles para la pestaña de usuarios
                var roles = await _usuarioService.GetRolesAsync();
                ViewBag.Roles = roles ?? new List<SelectListItem>();

                return View("Index", usuariosResponse);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error en IndexProductos: {ex.Message}");

                var usuariosResponse = new UsuarioListResponse
                {
                    Usuarios = new List<UsuarioResponse>(),
                    PaginaActual = 1,
                    TotalPaginas = 1,
                    TotalUsuarios = 0
                };

                var productosResponse = new ListProductoResponse
                {
                    Productos = new List<ProductoDto>(),
                    PaginaActual = 1,
                    TotalProductos = 0,
                    Categorias = new List<SelectListItem>()
                };

                ViewData["Productos"] = productosResponse;
                ViewBag.Roles = new List<SelectListItem>();

                return View("Index", usuariosResponse);
            }
        }

        // Método auxiliar para obtener la respuesta de productos
        private async Task<ListProductoResponse> GetProductosResponse(int pagina = 1, int cantidadPorPagina = 10,
                     string? busqueda = null, int? categoriaFiltro = null)
        {
            try
            {
                var productos = await _productoService.ListarTodosAsync();

                // Aplicar filtros
                var productosFiltrados = productos.Productos?.AsQueryable() ?? new List<ProductoDto>().AsQueryable();

                if (!string.IsNullOrEmpty(busqueda))
                {
                    productosFiltrados = productosFiltrados.Where(p =>
                        p.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                        (p.Descripcion != null && p.Descripcion.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                    );
                }

                if (categoriaFiltro.HasValue)
                {
                    productosFiltrados = productosFiltrados.Where(p => p.Id_Categoria == categoriaFiltro.Value);
                }

                // Aplicar paginación
                var totalProductos = productosFiltrados.Count();
                var productosPaginados = productosFiltrados
                    .Skip((pagina - 1) * cantidadPorPagina)
                    .Take(cantidadPorPagina)
                    .ToList();

                var response = new ListProductoResponse
                {
                    Productos = productosPaginados,
                    PaginaActual = pagina,
                    CantidadPorPagina = cantidadPorPagina,
                    TotalProductos = totalProductos,
                    Busqueda = busqueda,
                    CategoriaFiltro = categoriaFiltro
                };

                // Cargar categorías para el dropdown
                var categorias = await _categoriaService.ListarTodasAsync();
                response.Categorias = categorias?.Select(c => new SelectListItem
                {
                    Value = c.Id_Categoria.ToString(),
                    Text = c.Nombre
                }).ToList() ?? new List<SelectListItem>();

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetProductosResponse: {ex.Message}");
                return new ListProductoResponse
                {
                    Productos = new List<ProductoDto>(),
                    PaginaActual = pagina,
                    CantidadPorPagina = cantidadPorPagina,
                    TotalProductos = 0,
                    Busqueda = busqueda,
                    CategoriaFiltro = categoriaFiltro,
                    Categorias = new List<SelectListItem>()
                };
            }
        }

        [HttpGet]
        public async Task<IActionResult> CrearUsuario()
        {
            var roles = await _usuarioService.GetRolesAsync();
            ViewBag.Roles = roles ?? new List<SelectListItem>();

            var model = new UsuarioDto { Estado = true };
            return View("UsuarioForm", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditarUsuario(int id)
        {
            var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
            if (usuario == null)
            {
                return RedirectToAction("IndexUsuarios");
            }

            var roles = await _usuarioService.GetRolesAsync();
            ViewBag.Roles = roles ?? new List<SelectListItem>();

            return View("UsuarioForm", usuario);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerModalUsuario(string accion, int? usuarioId = null,
            string? busqueda = null, int? rolFiltro = null, int pagina = 1)
        {
            try
            {
                var model = new UsuarioDto();
                ViewBag.AccionModal = accion;

                ViewBag.BusquedaActual = busqueda;
                ViewBag.RolFiltroActual = rolFiltro;
                ViewBag.PaginaActual = pagina;

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
                    model.Estado = true;
                }

                return PartialView("_UsuarioModal", model);
            }
            catch
            {
                ViewBag.AccionModal = accion;
                ViewBag.Roles = new List<SelectListItem>();
                return PartialView("_UsuarioModal", new UsuarioDto());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerModalProducto(string accion, int? productoId = null,
            string? busqueda = null, int? categoriaFiltro = null, int pagina = 1)
        {
            try
            {
                var model = new ProductoDto();
                ViewBag.AccionModal = accion;

                ViewBag.BusquedaActual = busqueda;
                ViewBag.CategoriaFiltroActual = categoriaFiltro;
                ViewBag.PaginaActual = pagina;

                // Cargar categorías
                var categorias = await _categoriaService.ListarTodasAsync();
                ViewBag.Categorias = categorias?.Select(c => new SelectListItem
                {
                    Value = c.Id_Categoria.ToString(),
                    Text = c.Nombre
                }).ToList() ?? new List<SelectListItem>();

                if (productoId.HasValue && (accion == "editar" || accion == "ver"))
                {
                    var productos = await _productoService.ListarTodosAsync();
                    var producto = productos.Productos?.FirstOrDefault(p => p.Id_Producto == productoId.Value);
                    if (producto != null)
                    {
                        model = producto;
                    }
                }
                else if (accion == "crear")
                {
                    model.Estado = true;
                }

                return PartialView("_ProductoModal", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerModalProducto: {ex.Message}");
                ViewBag.AccionModal = accion;
                ViewBag.Categorias = new List<SelectListItem>();
                return PartialView("_ProductoModal", new ProductoDto());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarUsuario(
            [FromForm] UsuarioDto usuarioDto,
            [FromForm] string busqueda = null,
            [FromForm] int? rolFiltro = null,
            [FromForm] int pagina = 1,
            [FromForm] bool isAjax = false)
        {
            try
            {
                // Detectar AJAX por parámetro o por header
                isAjax = isAjax || Request.Headers["X-Requested-With"] == "XMLHttpRequest";

                if (usuarioDto.Id_Usr > 0 && string.IsNullOrEmpty(usuarioDto.Contrasena))
                {
                    usuarioDto.Contrasena = null;
                    usuarioDto.ConfirmarContrasena = null;
                    ModelState.Remove("Contrasena");
                    ModelState.Remove("ConfirmarContrasena");
                }

                if (!ModelState.IsValid)
                {
                    if (isAjax)
                    {
                        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                        return BadRequest(new { success = false, message = "Errores de validación", errors = errors });
                    }

                    var roles = await _usuarioService.GetRolesAsync();
                    ViewBag.Roles = roles ?? new List<SelectListItem>();
                    return View("UsuarioForm", usuarioDto);
                }

                bool result;
                if (usuarioDto.Id_Usr > 0)
                {
                    result = await _adminService.ActualizarUsuarioAsync(usuarioDto);
                }
                else
                {
                    result = await _adminService.CrearUsuarioAsync(usuarioDto);
                }

                if (result)
                {
                    var msg = usuarioDto.Id_Usr > 0 ? "Usuario actualizado correctamente" : "Usuario creado correctamente";
                    if (isAjax)
                    {
                         return Ok(new { success = true, message = msg });
                    }

                    SetAlert(msg, "success");
                    return RedirectToAction("IndexUsuarios", new { busqueda, rolFiltro, pagina });
                }
                else
                {
                    var errorMessage = "Error al guardar el usuario en la base de datos";
                    if (isAjax)
                    {
                         return BadRequest(new { success = false, message = errorMessage });
                    }

                    ModelState.AddModelError("", errorMessage);
                    var roles = await _usuarioService.GetRolesAsync();
                    ViewBag.Roles = roles ?? new List<SelectListItem>();
                    return View("UsuarioForm", usuarioDto);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "Error interno: " + ex.Message;
                if (isAjax)
                {
                     return StatusCode(500, new { success = false, message = errorMessage });
                }

                ModelState.AddModelError("", errorMessage);
                var roles = await _usuarioService.GetRolesAsync();
                ViewBag.Roles = roles ?? new List<SelectListItem>();
                return View("UsuarioForm", usuarioDto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarProducto(
            [FromForm] ProductoDto productoDto,
            IFormFile ImagenFile, 
            [FromForm] string busqueda = null,
            [FromForm] int? categoriaFiltro = null,
            [FromForm] int pagina = 1)
        {
            try
            {
                // Guardar imagen si se envió
                if (ImagenFile != null && ImagenFile.Length > 0)
                {
                    var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/productos");
                    if (!Directory.Exists(imagesPath))
                    {
                        Directory.CreateDirectory(imagesPath);
                    }
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImagenFile.FileName);
                    var filePath = Path.Combine(imagesPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImagenFile.CopyToAsync(stream);
                    }
                    productoDto.Imagen = "/images/productos/" + fileName;
                }

                // Resto del método igual...
                bool success;
                string message;

                if (productoDto.Id_Producto > 0)
                {
                    var resultadoEditar = await _productoService.EditarAsync(productoDto);
                    success = resultadoEditar.Success;
                    message = resultadoEditar.Message;
                }
                else
                {
                    var resultadoRegistrar = await _productoService.RegisterAsync(productoDto);
                    success = resultadoRegistrar.Success;
                    message = resultadoRegistrar.Message;
                }

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = productoDto.Id_Producto > 0 ? "Producto actualizado correctamente" : "Producto creado correctamente"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = message,
                        errors = new List<string> { message }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción en GuardarProducto: {ex}");

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
                    SetAlert("Usuario eliminado correctamente.", "success");
                else
                    SetAlert("No se pudo eliminar el usuario.", "error");
            }
            catch (Exception ex)
            {
                SetAlert($"Error al eliminar el usuario: {ex.Message}", "error");
            }

            return RedirectToAction("IndexUsuarios", new { busqueda, rolFiltro, pagina });
        }

        [HttpGet]
        public async Task<IActionResult> EliminarProducto(int id, string? busqueda, int? categoriaFiltro, int pagina = 1)
        {
            try
            {
                var resultado = await _productoService.EliminarAsync(id);

                if (resultado.Success)
                    TempData["MensajeExito"] = "Producto eliminado correctamente.";
                else
                    TempData["MensajeError"] = resultado.Message;
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al eliminar el producto: {ex.Message}";
            }

            return RedirectToAction("IndexProductos", new { busqueda, categoriaFiltro, pagina });
        }

        [HttpGet]
        public async Task<IActionResult> DeshabilitarProducto(int id, string? busqueda, int? categoriaFiltro, int pagina = 1)
        {
            try
            {
                var resultado = await _productoService.DeshabilitarAsync(id);

                if (resultado.Success)
                    TempData["MensajeExito"] = "Producto deshabilitado correctamente.";
                else
                    TempData["MensajeError"] = resultado.Message;
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al deshabilitar el producto: {ex.Message}";
            }

            return RedirectToAction("IndexProductos", new { busqueda, categoriaFiltro, pagina });
        }

        [HttpGet]
        public async Task<IActionResult> HabilitarProducto(int id, string? busqueda, int? categoriaFiltro, int pagina = 1)
        {
            try
            {
                var resultado = await _productoService.HabilitarAsync(id);

                if (resultado.Success)
                    TempData["MensajeExito"] = "Producto habilitado correctamente.";
                else
                    TempData["MensajeError"] = resultado.Message;
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al habilitar el producto: {ex.Message}";
            }

            return RedirectToAction("IndexProductos", new { busqueda, categoriaFiltro, pagina });
        }

        [HttpGet]
        public async Task<IActionResult> ReactivarUsuario(int id, string? busqueda, int? rolFiltro, bool mostrarInactivos, int pagina = 1)
        {
            try
            {
                bool resultado = await _adminService.ReactivarUsuarioAsync(id);

                if (resultado)
                    SetAlert("Usuario reactivado correctamente.", "success");
                else
                    SetAlert("No se pudo reactivar el usuario.", "error");
            }
            catch (Exception ex)
            {
                SetAlert($"Error al reactivar el usuario: {ex.Message}", "error");
            }

            return RedirectToAction("IndexUsuarios", new { busqueda, rolFiltro, mostrarInactivos, pagina });
        }
    }
}
