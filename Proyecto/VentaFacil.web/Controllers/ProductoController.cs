using Microsoft.AspNetCore.Mvc;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services.Producto;

namespace VentaFacil.web.Controllers
{
    public class ProductoController : Controller
    {
        private readonly IRegisterProductoService _registerProductoService;
        private readonly IListProductoService _listProductoService;
        private readonly IEditProductoService _editProductoService;

        public ProductoController(
            IRegisterProductoService registerProductoService,
            IListProductoService listProductoService,
            IEditProductoService editProductoService)
        {
            _registerProductoService = registerProductoService;
            _listProductoService = listProductoService;
            _editProductoService = editProductoService;
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(ProductoDto producto)
        {
            if (!ModelState.IsValid)
                return View(producto);

            var resultado = await _registerProductoService.RegisterAsync(producto);

            if (resultado.Success)
            {
                TempData["Mensaje"] = resultado.Message;
                return RedirectToAction("Listar");
            }

            ModelState.AddModelError(string.Empty, resultado.Message);
            return View(producto);
        }

        public async Task<IActionResult> Listar()
        {
            var response = await _listProductoService.ListarTodosAsync();
            return View(response);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var response = await _listProductoService.ListarTodosAsync();
            var producto = response.Productos.FirstOrDefault(p => p.Id_Producto == id);

            if (producto == null)
                return NotFound();

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(ProductoDto producto)
        {
            if (!ModelState.IsValid)
                return View(producto);

            var resultado = await _editProductoService.EditarAsync(producto);

            if (resultado.Success)
            {
                TempData["Mensaje"] = resultado.Message;
                return RedirectToAction("Listar");
            }

            ModelState.AddModelError(string.Empty, resultado.Message);
            return View(producto);
        }
    }
}