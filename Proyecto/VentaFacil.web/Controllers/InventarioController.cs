using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services.Inventario;
using VentaFacil.web.Services.Movimiento;
using VentaFacil.web.Services.PDF;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class InventarioController : Controller
    {
        private readonly IInventarioService _inventarioService;
        private readonly IMovimientoService _movimientoService;
        private readonly IPdfService _pdfService;

        public InventarioController(IInventarioService inventarioService, IMovimientoService movimientoService, IPdfService pdfService)
        {
            _inventarioService = inventarioService;
            _movimientoService = movimientoService;
            _pdfService = pdfService;
        }

        // GET: Inventario/Listar
        public async Task<IActionResult> Listar()
        {
            var inventarios = await _inventarioService.ListarTodosAsync();
            return View(inventarios);
        }
        // SIN USAR 
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
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }
                return View(dto);
            }

            var result = await _inventarioService.RegistrarAsync(dto);
            if (result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Listar));
            }

            ModelState.AddModelError("", "No se pudo crear el inventario.");
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, errors = new[] { "No se pudo crear el inventario." } });
            }
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
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }
                return View(dto);
            }

            if (dto.StockActual < 0)
                ModelState.AddModelError(nameof(dto.StockActual), "El stock actual no puede ser negativo.");

            if (dto.StockMinimo < 0)
                ModelState.AddModelError(nameof(dto.StockMinimo), "El stock mínimo no puede ser negativo.");

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }
                return View(dto);
            }

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 1;
            var result = await _inventarioService.EditarAsync(dto, usuarioId);
            if (result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Listar));
            }

            ModelState.AddModelError("", "No se pudo editar el inventario.");
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, errors = new[] { "No se pudo editar el inventario." } });
            }
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

            TempData["Error"] = "No se pudo eliminar el inventario.";
            return RedirectToAction(nameof(Listar));
        }

        [HttpPost]
        public async Task<IActionResult> AgregarUnidad(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 1;
            var result = await _inventarioService.AgregarUnidadAsync(id, usuarioId);
            if (!result)
                return NotFound();
            return RedirectToAction("Listar");
        }

        [HttpPost]
        public async Task<IActionResult> QuitarUnidad(int id)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 1;
            var result = await _inventarioService.QuitarUnidadAsync(id, usuarioId);
            if (!result)
            {
                TempData["Error"] = "No se puede registrar la salida. El inventario no puede ser negativo.";
                return RedirectToAction("Listar");
            }
            return RedirectToAction("Listar");
        }

        // GET: Inventario/RegistrarEntrada
        public async Task<IActionResult> RegistrarEntrada()
        {
            var inventarios = await _inventarioService.ListarTodosAsync();
            var viewModel = new VentaFacil.web.Models.ViewModel.RegistrarEntradaViewModel
            {
                Inventarios = inventarios.Select(i => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = i.Id_Inventario.ToString(),
                    Text = i.Nombre
                })
            };
            return View(viewModel);
        }

        // POST: Inventario/RegistrarEntrada
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEntrada(VentaFacil.web.Models.ViewModel.RegistrarEntradaViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                     return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                var inventarios = await _inventarioService.ListarTodosAsync();
                viewModel.Inventarios = inventarios.Select(i => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = i.Id_Inventario.ToString(),
                    Text = i.Nombre
                });
                return View(viewModel);
            }

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 1; 
            var result = await _inventarioService.RegistrarEntradaAsync(viewModel.IdInventario, viewModel.Cantidad, viewModel.Observaciones, usuarioId);

            if (result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    TempData["Success"] = "Entrada registrada correctamente.";
                    return Json(new { success = true });
                }
                TempData["Success"] = "Entrada registrada correctamente.";
                return RedirectToAction(nameof(Listar));
            }

            ModelState.AddModelError("", "No se pudo registrar la entrada.");
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                 return Json(new { success = false, errors = new[] { "No se pudo registrar la entrada." } });
            }

            var invs = await _inventarioService.ListarTodosAsync();
            viewModel.Inventarios = invs.Select(i => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = i.Id_Inventario.ToString(),
                Text = i.Nombre
            });
            return View(viewModel);
        }

        // POST: Inventario/RegistrarSalida
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarSalida(VentaFacil.web.Models.ViewModel.RegistrarEntradaViewModel viewModel)
        {
            // Usamos el mismo ViewModel ya que los campos son idénticos (Id, Cantidad, Observaciones)
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                     return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }
                return RedirectToAction(nameof(Listar)); // Fallback simple para salida si no es ajax
            }

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 1; 
            var result = await _inventarioService.RegistrarSalidaAsync(viewModel.IdInventario, viewModel.Cantidad, viewModel.Observaciones, usuarioId);

            if (result)
            {
                TempData["Success"] = "Salida registrada correctamente.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }
                return RedirectToAction(nameof(Listar));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                 return Json(new { success = false, errors = new[] { "No se pudo registrar la salida. Verifique si hay stock suficiente." } });
            }

            TempData["Error"] = "No se pudo registrar la salida (Stock insuficiente).";
            return RedirectToAction(nameof(Listar));
        }

        // GET: Inventario/HistorialMovimientos
        public async Task<IActionResult> HistorialMovimientos(int idInventario, DateTime? fechaInicio, DateTime? fechaFin, string? tipoMovimiento)
        {
            var movimientos = await _movimientoService.ListarMovimientosAsync(idInventario, fechaInicio, fechaFin);
            
            if (!string.IsNullOrEmpty(tipoMovimiento))
            {
                movimientos = movimientos.Where(m => m.Tipo_Movimiento.Contains(tipoMovimiento)).ToList();
            }

            ViewBag.IdInventario = idInventario;
            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;
            ViewBag.TipoMovimiento = tipoMovimiento;
            
            return View(movimientos);
        }

        public async Task<IActionResult> ExportarHistorialPdf(int idInventario, DateTime? fechaInicio, DateTime? fechaFin, string? tipoMovimiento)
        {
            var movimientos = await _movimientoService.ListarMovimientosAsync(idInventario, fechaInicio, fechaFin);
            if (!string.IsNullOrEmpty(tipoMovimiento))
            {
                movimientos = movimientos.Where(m => m.Tipo_Movimiento.Contains(tipoMovimiento)).ToList();
            }

            var inventario = await _inventarioService.GetByIdAsync(idInventario);
            var nombreInsumo = inventario?.Nombre ?? "Desconocido";

            var pdfBytes = _pdfService.GenerarHistorialMovimientosPdf(movimientos, nombreInsumo);
            return File(pdfBytes, "application/pdf", $"Historial_{nombreInsumo}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> ExportarHistorialExcel(int idInventario, DateTime? fechaInicio, DateTime? fechaFin, string? tipoMovimiento)
        {
            var movimientos = await _movimientoService.ListarMovimientosAsync(idInventario, fechaInicio, fechaFin);
            if (!string.IsNullOrEmpty(tipoMovimiento))
            {
                movimientos = movimientos.Where(m => m.Tipo_Movimiento.Contains(tipoMovimiento)).ToList();
            }

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("ID,Tipo,Cantidad,Fecha,Usuario");

            foreach (var mov in movimientos)
            {
                builder.AppendLine($"{mov.Id_Movimiento},{mov.Tipo_Movimiento},{mov.Cantidad},{mov.Fecha},{mov.Id_Usuario}");
            }

            var content = builder.ToString();
            var buffer = System.Text.Encoding.UTF8.GetBytes(content);
            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var result = new byte[preamble.Length + buffer.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(buffer, 0, result, preamble.Length, buffer.Length);

            return File(result, "text/csv", $"Historial_{idInventario}_{DateTime.Now:yyyyMMdd}.csv");
        }

        // GET: Inventario/CorregirMovimiento/5
        [Authorize(Roles = "Administrador,EncargadoInventario")]
        public async Task<IActionResult> CorregirMovimiento(int id)
        {
            
            var movimientos = await _movimientoService.ListarMovimientosAsync(null, null, null);
            var mov = movimientos.FirstOrDefault(m => m.Id_Movimiento == id);
            
            if (mov == null) return NotFound();

            return View(mov);
        }

        // POST: Inventario/CorregirMovimiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,EncargadoInventario")]
        public async Task<IActionResult> CorregirMovimiento(int Id_Movimiento, int Cantidad, string Tipo_Movimiento, string Motivo)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
            if (usuarioId == 0) return RedirectToAction("InicioSesion", "Login");

            var result = await _movimientoService.CorregirMovimientoAsync(Id_Movimiento, Cantidad, Tipo_Movimiento, Motivo, usuarioId);

            if (result)
            {
                TempData["Success"] = "Movimiento corregido correctamente.";
                return RedirectToAction(nameof(Listar));
            }

            TempData["Error"] = "No se pudo corregir el movimiento.";
            return RedirectToAction(nameof(Listar));
        }

        [HttpGet]
        public async Task<IActionResult> NotificacionesStockMinimo()
        {
            var notificaciones = await _inventarioService.ObtenerStockMinimoAsync();
            return View(notificaciones);
        }

        [HttpGet]
        public async Task<IActionResult> NotificacionesStockMinimoJson()
        {
            var notificaciones = await _inventarioService.ObtenerStockMinimoAsync();
            return Json(notificaciones);
        }
    }
}