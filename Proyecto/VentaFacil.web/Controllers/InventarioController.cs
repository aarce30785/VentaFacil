using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services.Inventario;
using VentaFacil.web.Services.Producto;
using System.Threading.Tasks;
using System.Linq;

namespace VentaFacil.web.Controllers
{
    public class InventarioController : Controller
    {
        private readonly IRegisterInventarioService _registerInventarioService;
        private readonly IEditInventarioService _editInventarioService;
        private readonly IListInventarioService _listInventarioService;
        private readonly IGetInventarioService _getInventarioService;
        private readonly IListProductoService _listProductoService;

        public InventarioController(
            IRegisterInventarioService registerInventarioService,
            IEditInventarioService editInventarioService,
            IListInventarioService listInventarioService,
            IGetInventarioService getInventarioService,
            IListProductoService listProductoService)
        {
            _registerInventarioService = registerInventarioService;
            _editInventarioService = editInventarioService;
            _listInventarioService = listInventarioService;
            _getInventarioService = getInventarioService;
            _listProductoService = listProductoService;
        }

        // GET: Inventario/Listar
        public async Task<IActionResult> Listar()
        {
            var response = await _listInventarioService.ListarTodosAsync();
            return View(response.Inventarios);
        }

        // GET: Inventario/Registrar
        public async Task<IActionResult> Registrar()
        {
            var productos = await _listProductoService.ListarTodosAsync();
            ViewBag.Productos = productos.Productos
                .Select(p => new SelectListItem { Value = p.Id_Producto.ToString(), Text = p.Nombre })
                .ToList();
            return View();
        }

        // POST: Inventario/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(InventarioDto dto)
        {
            if (!ModelState.IsValid)
            {
                var productos = await _listProductoService.ListarTodosAsync();
                ViewBag.Productos = productos.Productos
                    .Select(p => new SelectListItem { Value = p.Id_Producto.ToString(), Text = p.Nombre })
                    .ToList();
                return View(dto);
            }

            var response = await _registerInventarioService.RegisterAsync(dto);
            if (response.Success)
            {
                TempData["Mensaje"] = response.Message;
                return RedirectToAction(nameof(Listar));
            }

            ModelState.AddModelError("", response.Message);
            var productosError = await _listProductoService.ListarTodosAsync();
            ViewBag.Productos = productosError.Productos
                .Select(p => new SelectListItem { Value = p.Id_Producto.ToString(), Text = p.Nombre })
                .ToList();
            return View(dto);
        }

        // GET: Inventario/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var getResponse = await _getInventarioService.GetByIdAsync(id);
            if (!getResponse.Success || getResponse.Inventario == null)
                return NotFound();

            var productos = await _listProductoService.ListarTodosAsync();
            ViewBag.Productos = productos.Productos
                .Select(p => new SelectListItem { Value = p.Id_Producto.ToString(), Text = p.Nombre })
                .ToList();

            return View(getResponse.Inventario);
        }

        // POST: Inventario/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(InventarioDto dto)
        {
            if (!ModelState.IsValid)
            {
                var productos = await _listProductoService.ListarTodosAsync();
                ViewBag.Productos = productos.Productos
                    .Select(p => new SelectListItem { Value = p.Id_Producto.ToString(), Text = p.Nombre })
                    .ToList();
                return View(dto);
            }

            var response = await _editInventarioService.EditarAsync(dto);
            if (response.Success)
            {
                TempData["Mensaje"] = response.Message;
                return RedirectToAction(nameof(Listar));
            }

            ModelState.AddModelError("", response.Message);
            var productosError = await _listProductoService.ListarTodosAsync();
            ViewBag.Productos = productosError.Productos
                .Select(p => new SelectListItem { Value = p.Id_Producto.ToString(), Text = p.Nombre })
                .ToList();
            return View(dto);
        }

        // GET: Inventario/Buscar
        public async Task<IActionResult> Buscar(string? nombre, int? id)
        {
            var response = await _listInventarioService.ListarTodosAsync();
            var inventarios = response.Inventarios.AsQueryable();

            if (id.HasValue)
                inventarios = inventarios.Where(i => i.Id_Inventario == id.Value);

            if (!string.IsNullOrEmpty(nombre))
                inventarios = inventarios.Where(i => i.Id_Producto.ToString() == nombre);

            return View(inventarios.ToList());
        }
    }
}