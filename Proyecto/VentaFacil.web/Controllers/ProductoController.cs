using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Producto;
using VentaFacil.web.Services.Producto;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class ProductoController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly ICategoriaService _categoriaService;

        public ProductoController(
            IProductoService registerProductoService,
            ICategoriaService categoriaService)
        {
            _productoService = registerProductoService;
            _categoriaService = categoriaService;
        }

        [HttpGet]
        public async Task<IActionResult> Listar(int pagina = 1, int cantidadPorPagina = 10,
                     string? busqueda = null, int? categoriaFiltro = null, int? productoId = null, string? accion = null, bool mostrarInactivos = false)
        {
             try
            {
                // Cargar productos con filtros
                ViewBag.MostrarInactivos = mostrarInactivos;
                var productosResponse = await GetProductosResponse(pagina, cantidadPorPagina, busqueda, categoriaFiltro, mostrarInactivos);
                return View(productosResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Listar: {ex.Message}");
                var emptyResponse = new ListProductoResponse
                {
                    Productos = new List<ProductoDto>(),
                    PaginaActual = 1,
                    TotalProductos = 0,
                    Categorias = new List<SelectListItem>()
                };
                return View(emptyResponse);
            }
        }

        // Método auxiliar para obtener la respuesta de productos (Copiado de AdminController)
        private async Task<ListProductoResponse> GetProductosResponse(int pagina = 1, int cantidadPorPagina = 10,
                     string? busqueda = null, int? categoriaFiltro = null, bool mostrarInactivos = false)
        {
            try
            {
                var productos = await _productoService.ListarTodosAsync();

                if (!productos.Success)
                {
                    return new ListProductoResponse
                    {
                        Success = false,
                        Message = productos.Message,
                        Productos = new List<ProductoDto>(),
                        PaginaActual = pagina,
                        CantidadPorPagina = cantidadPorPagina,
                        TotalProductos = 0,
                        Busqueda = busqueda,
                        CategoriaFiltro = categoriaFiltro,
                        MostrarInactivos = mostrarInactivos,
                        Categorias = await GetCategoriasSelectList()
                    };
                }

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

                if (!mostrarInactivos)
                {
                    productosFiltrados = productosFiltrados.Where(p => p.Estado);
                }

                // Ordenar por ID descendente (nuevos primero)
                productosFiltrados = productosFiltrados.OrderByDescending(p => p.Id_Producto);

                // Aplicar paginación
                var totalProductos = productosFiltrados.Count();
                var productosPaginados = productosFiltrados
                    .Skip((pagina - 1) * cantidadPorPagina)
                    .Take(cantidadPorPagina)
                    .ToList();

                var response = new ListProductoResponse
                {
                    Success = true,
                    Productos = productosPaginados,
                    PaginaActual = pagina,
                    CantidadPorPagina = cantidadPorPagina,
                    TotalProductos = totalProductos,
                    Busqueda = busqueda,
                    CategoriaFiltro = categoriaFiltro,
                    MostrarInactivos = mostrarInactivos
                };

                // Cargar categorías para el dropdown
                response.Categorias = await GetCategoriasSelectList();

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
                    MostrarInactivos = mostrarInactivos,
                    Categorias = new List<SelectListItem>()
                };
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerModalProducto(string accion, int? productoId = null,
            string? busqueda = null, int? categoriaFiltro = null, int pagina = 1, bool mostrarInactivos = false)
        {
            try
            {
                var model = new ProductoDto();
                ViewBag.AccionModal = accion;

                ViewBag.BusquedaActual = busqueda;
                ViewBag.CategoriaFiltroActual = categoriaFiltro;
                ViewBag.PaginaActual = pagina;
                ViewBag.MostrarInactivos = mostrarInactivos;

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
                    model.Estado = true; // Por defecto activo
                }

                return PartialView("~/Views/Shared/_ProductoModal.cshtml", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerModalProducto: {ex.Message}");
                ViewBag.AccionModal = accion;
                ViewBag.Categorias = new List<SelectListItem>();
                return PartialView("~/Views/Shared/_ProductoModal.cshtml", new ProductoDto());
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
                if (!ModelState.IsValid)
                {
                    var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault();
                    return BadRequest(new
                    {
                        success = false,
                        message = firstError?.ErrorMessage ?? "Por favor, revise los datos del formulario.",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

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
        public async Task<IActionResult> EliminarProducto(int id, string? busqueda, int? categoriaFiltro, int pagina = 1, bool mostrarInactivos = false)
        {
            try
            {
                var resultado = await _productoService.EliminarAsync(id);

                if (resultado.Success)
                    TempData["Success"] = "Producto eliminado correctamente.";
                else
                    TempData["Error"] = resultado.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar el producto: {ex.Message}";
            }

            return RedirectToAction("Listar", new { busqueda, categoriaFiltro, pagina, mostrarInactivos });
        }

        [HttpGet]
        public async Task<IActionResult> DeshabilitarProducto(int id, string? busqueda, int? categoriaFiltro, int pagina = 1, bool mostrarInactivos = false)
        {
            try
            {
                var resultado = await _productoService.DeshabilitarAsync(id);

                if (resultado.Success)
                    TempData["Success"] = "Producto deshabilitado correctamente.";
                else
                    TempData["Error"] = resultado.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al deshabilitar el producto: {ex.Message}";
            }

            return RedirectToAction("Listar", new { busqueda, categoriaFiltro, pagina, mostrarInactivos });
        }

        [HttpGet]
        public async Task<IActionResult> HabilitarProducto(int id, string? busqueda, int? categoriaFiltro, int pagina = 1, bool mostrarInactivos = false)
        {
            try
            {
                var resultado = await _productoService.HabilitarAsync(id);

                if (resultado.Success)
                    TempData["Success"] = "Producto habilitado correctamente.";
                else
                    TempData["Error"] = resultado.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al habilitar el producto: {ex.Message}";
            }

            return RedirectToAction("Listar", new { busqueda, categoriaFiltro, pagina, mostrarInactivos });
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarStock([FromBody] Dictionary<string, int> data)
        {
            try
            {
                if (!data.ContainsKey("idProducto") || !data.ContainsKey("cantidad"))
                {
                    return BadRequest(new { success = false, message = "Datos inválidos." });
                }

                int idProducto = data["idProducto"];
                int cantidad = data["cantidad"];

                var resultado = await _productoService.ActualizarStockAsync(idProducto, cantidad);

                if (resultado.Success)
                {
                    return Ok(new { success = true, message = resultado.Message });
                }
                else
                {
                    return BadRequest(new { success = false, message = resultado.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error interno: {ex.Message}" });
            }
        }

        private async Task<List<SelectListItem>> GetCategoriasSelectList()
        {
            var categorias = await _categoriaService.ListarTodasAsync();
            return categorias?.Select(c => new SelectListItem
            {
                Value = c.Id_Categoria.ToString(),
                Text = c.Nombre
            }).ToList() ?? new List<SelectListItem>();
        }
    }
}