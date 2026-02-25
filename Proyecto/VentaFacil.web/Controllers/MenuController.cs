using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Services.Producto;
using VentaFacil.web.Models.ViewModel;

namespace VentaFacil.web.Controllers
{
    [AllowAnonymous]
    public class MenuController : Controller
    {
        private readonly IProductoService _productoService;
        private readonly ICategoriaService _categoriaService;

        public MenuController(IProductoService productoService, ICategoriaService categoriaService)
        {
            _productoService = productoService;
            _categoriaService = categoriaService;
        }

        public async Task<IActionResult> Index(int? categoriaId, string busqueda)
        {
            var categorias = await _categoriaService.ListarTodasAsync();
            var response = await _productoService.ListarActivosAsync();
            
            var productos = response.Productos ?? new List<Models.Dto.ProductoDto>();

            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                productos = productos.Where(p => p.Id_Categoria == categoriaId.Value).ToList();
            }

            if (!string.IsNullOrEmpty(busqueda))
            {
                productos = productos.Where(p => p.Nombre.Contains(busqueda, System.StringComparison.OrdinalIgnoreCase) || 
                                               (p.Descripcion != null && p.Descripcion.Contains(busqueda, System.StringComparison.OrdinalIgnoreCase))).ToList();
            }

            var viewModel = new MenuViewModel
            {
                Productos = productos,
                Categorias = categorias,
                CategoriaSeleccionadaId = categoriaId,
                Busqueda = busqueda
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var producto = await _productoService.ObtenerPorIdAsync(id);

            // Validar que exista y que esté activo (PO-1102)
            if (producto == null || !producto.Estado)
            {
                return NotFound("Producto no disponible.");
            }

            return PartialView("_DetalleProductoModal", producto);
        }
    }
}
