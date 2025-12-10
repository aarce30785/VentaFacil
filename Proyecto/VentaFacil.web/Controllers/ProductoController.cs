using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VentaFacil.web.Models.Dto;
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
        public async Task<IActionResult> Registrar()
        {
            var categorias = await _categoriaService.ListarTodasAsync();
            ViewBag.Categorias = categorias.Select(c => new SelectListItem
            {
                Value = c.Id_Categoria.ToString(),
                Text = c.Nombre
            }).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(ProductoDto producto)
        {
            if (!ModelState.IsValid)
            {
                var categorias = await _categoriaService.ListarTodasAsync();
                ViewBag.Categorias = new SelectList(categorias, "Id_Categoria", "Nombre");
                return View(producto);
            }

            var resultado = await _productoService.RegisterAsync(producto);

            if (resultado.Success)
            {
                TempData["Mensaje"] = resultado.Message;
                return RedirectToAction("Listar");
            }

            ModelState.AddModelError(string.Empty, resultado.Message);
            var categoriasError = await _categoriaService.ListarTodasAsync();
            ViewBag.Categorias = new SelectList(categoriasError, "Id_Categoria", "Nombre");
            return View(producto);
        }

        public async Task<IActionResult> Listar()
        {
            var response = await _productoService.ListarTodosAsync();
            return View(response.Productos);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var response = await _productoService.ListarTodosAsync();
            var producto = response.Productos.FirstOrDefault(p => p.Id_Producto == id);

            if (producto == null)
                return NotFound();

            var categorias = await _categoriaService.ListarTodasAsync();
            ViewBag.Categorias = categorias.Select(c => new SelectListItem
            {
                Value = c.Id_Categoria.ToString(),
                Text = c.Nombre
            }).ToList();

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(ProductoDto producto)
        {
            if (!ModelState.IsValid)
            {
                var categorias = await _categoriaService.ListarTodasAsync();
                ViewBag.Categorias = categorias.Select(c => new SelectListItem
                {
                    Value = c.Id_Categoria.ToString(),
                    Text = c.Nombre
                }).ToList();
                return View(producto);
            }

            var resultado = await _productoService.EditarAsync(producto);

            if (resultado.Success)
            {
                TempData["Mensaje"] = resultado.Message;
                return RedirectToAction("Listar");
            }

            ModelState.AddModelError(string.Empty, resultado.Message);
            var categoriasError = await _categoriaService.ListarTodasAsync();
            ViewBag.Categorias = categoriasError.Select(c => new SelectListItem
            {
                Value = c.Id_Categoria.ToString(),
                Text = c.Nombre
            }).ToList();
            return View(producto);
        }
    }
}