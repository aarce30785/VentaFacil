using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services.Inventario;
using VentaFacil.web.Services.Movimiento;
namespace VentaFacil.web.Controllers
{
    public class InventarioController : Controller
    {
        private readonly IInventarioService _inventarioService;
        private readonly IMovimientoService _movimientoService;

        public InventarioController(IInventarioService inventarioService, IMovimientoService movimientoService)
        {
            _inventarioService = inventarioService;
            _movimientoService = movimientoService;
        }

        // GET: Inventario/Listar
        public async Task<IActionResult> Listar()
        {
            var inventarios = await _inventarioService.ListarTodosAsync();
            return View(inventarios);
        }

        // GET: Inventario/Details/5
        public async Task<IActionResult> Detalles(int id)
        {
            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound();
            return View(inventario);
        }

        // GET: Inventario/Create
        public IActionResult Registrar()
        {
            return View();
        }

        // POST: Inventario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(InventarioDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _inventarioService.RegistrarAsync(dto);
            if (result)
                return RedirectToAction(nameof(Listar));

            ModelState.AddModelError("", "No se pudo crear el inventario.");
            return View(dto);
        }

        // GET: Inventario/Edit/5
        public async Task<IActionResult> Editar(int id)
        {
            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound();
            return View(inventario);
        }

        // POST: Inventario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(InventarioDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (dto.StockActual < 0)
                ModelState.AddModelError(nameof(dto.StockActual), "El stock actual no puede ser negativo.");

            if (dto.StockMinimo < 0)
                ModelState.AddModelError(nameof(dto.StockMinimo), "El stock mínimo no puede ser negativo.");

            if (!ModelState.IsValid)
                return View(dto);

            var result = await _inventarioService.EditarAsync(dto);
            if (result)
                return RedirectToAction(nameof(Listar));

            ModelState.AddModelError("", "No se pudo editar el inventario.");
            return View(dto);
        }

        // GET: Inventario/Delete/5
        public async Task<IActionResult> Eliminar(int id)
        {
            var inventario = await _inventarioService.GetByIdAsync(id);
            if (inventario == null)
                return NotFound();
            return View(inventario);
        }

        // POST: Inventario/Delete/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _inventarioService.EliminarAsync(id);
            if (result)
                return RedirectToAction(nameof(Listar));

            ModelState.AddModelError("", "No se pudo eliminar el inventario.");
            var inventario = await _inventarioService.GetByIdAsync(id);
            return View("Delete", inventario);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarUnidad(int id)
        {
            var result = await _inventarioService.AgregarUnidadAsync(id);
            if (!result)
                return NotFound();
            return RedirectToAction("Listar");
        }

        [HttpPost]
        public async Task<IActionResult> QuitarUnidad(int id)
        {
            var result = await _inventarioService.QuitarUnidadAsync(id);
            if (!result)
            {
                TempData["Error"] = "No se puede registrar la salida. El inventario no puede ser negativo.";
                return RedirectToAction("Listar");
            }
            return RedirectToAction("Listar");
        }

        // GET: Inventario/HistorialMovimientos
        public async Task<IActionResult> HistorialMovimientos(int idInventario, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var movimientos = await _movimientoService.ListarMovimientosAsync(idInventario, fechaInicio, fechaFin);
            ViewBag.IdInventario = idInventario;
            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;
            return View(movimientos);
        }
    }
}
