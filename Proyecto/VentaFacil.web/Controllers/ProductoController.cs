using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services.Producto;

namespace VentaFacil.web.Controllers
{
    public class ProductoController : Controller
    {
        private readonly IRegisterProductoService _registerProductoService;
        private readonly IListProductoService _listProductoService;
        private readonly IEditProductoService _editProductoService;
        private readonly ICategoriaService _categoriaService;

        public ProductoController(
            IRegisterProductoService registerProductoService,
            IListProductoService listProductoService,
            IEditProductoService editProductoService,
            ICategoriaService categoriaService)
        {
            _registerProductoService = registerProductoService;
            _listProductoService = listProductoService;
            _editProductoService = editProductoService;
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

            var resultado = await _registerProductoService.RegisterAsync(producto);

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
            var response = await _listProductoService.ListarTodosAsync();
            return View(response.Productos);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var response = await _listProductoService.ListarTodosAsync();
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

            var resultado = await _editProductoService.EditarAsync(producto);

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