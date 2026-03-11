using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Inventario;
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
        private readonly ApplicationDbContext _context;

        public InventarioController(IInventarioService inventarioService, IMovimientoService movimientoService, IPdfService pdfService, ApplicationDbContext context)
        {
            _inventarioService = inventarioService;
            _movimientoService = movimientoService;
            _pdfService = pdfService;
            _context = context;
        }

        // GET: Inventario/Listar
        // GET: Inventario/Listar
        public async Task<IActionResult> Listar(string? busqueda = null, int pagina = 1, int cantidadPorPagina = 10, bool mostrarInactivos = false)
        {
            var inventarios = await _inventarioService.ListarTodosAsync(mostrarInactivos);
            
            // Filtrado
            if (!string.IsNullOrEmpty(busqueda))
            {
                inventarios = inventarios.Where(i => 
                    i.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            var totalRegistros = inventarios.Count();
            var totalPaginas = (int)Math.Ceiling((double)totalRegistros / cantidadPorPagina);
            
            // Paginación
            var inventariosPaginados = inventarios
                .Skip((pagina - 1) * cantidadPorPagina)
                .Take(cantidadPorPagina)
                .OrderByDescending(i => i.Id_Inventario)
                .ToList();

            var response = new ListInventarioResponse
            {
                Success = true,
                Inventarios = inventariosPaginados,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                CantidadPorPagina = cantidadPorPagina,
                TotalRegistros = totalRegistros,
                Busqueda = busqueda,
                TotalInsumos = inventarios.Count,
                StockBajo = inventarios.Count(i => i.StockMinimo > 0 && i.StockActual <= i.StockMinimo),
                MostrarInactivos = mostrarInactivos
            };

            return View(response);
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

            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 1;
            var result = await _inventarioService.RegistrarAsync(dto, usuarioId);
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

        // POST: Inventario/HabilitarInsumo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HabilitarInsumo(int id, string? busqueda, int pagina = 1)
        {
            var result = await _inventarioService.HabilitarAsync(id);
            if (result)
                TempData["Success"] = "Insumo habilitado correctamente.";
            else
                TempData["Error"] = "No se pudo habilitar el insumo.";

            return RedirectToAction(nameof(Listar), new { busqueda, pagina, mostrarInactivos = true });
        }

        // GET: Inventario/ConfiguracionAlertas — carga el partial modal vía AJAX
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ConfiguracionAlertas()
        {
            var config = await _context.ConfiguracionNotificacion.FirstOrDefaultAsync()
                         ?? new ConfiguracionNotificacion();
            return PartialView("_ConfigAlertasModal", config);
        }

        // POST: Inventario/GuardarConfiguracionAlertas
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GuardarConfiguracionAlertas(ConfiguracionNotificacion model)
        {
            try
            {
                // Si el toggle está apagado, limpiar el correo
                if (!model.AlertaStockEmail)
                    model.CorreoDestino = null;

                // Validar que haya correo si el toggle está activo
                if (model.AlertaStockEmail && string.IsNullOrWhiteSpace(model.CorreoDestino))
                {
                    TempData["Error"] = "Debe ingresar un correo destino para activar las alertas por email.";
                    return RedirectToAction(nameof(Listar));
                }

                var existing = await _context.ConfiguracionNotificacion.FirstOrDefaultAsync();
                if (existing == null)
                {
                    model.FechaActualizacion = DateTime.Now;
                    _context.ConfiguracionNotificacion.Add(model);
                }
                else
                {
                    existing.AlertaStockEmail  = model.AlertaStockEmail;
                    existing.CorreoDestino     = model.CorreoDestino;
                    existing.FechaActualizacion = DateTime.Now;
                    _context.ConfiguracionNotificacion.Update(existing);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = model.AlertaStockEmail
                    ? $"Alertas de stock por email activadas. Se enviará a: {model.CorreoDestino}"
                    : "Alertas de stock por email desactivadas.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al guardar la configuración: " + ex.Message;
            }

            return RedirectToAction(nameof(Listar));
        }


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
            if (string.IsNullOrWhiteSpace(viewModel.Observaciones))
            {
                ModelState.AddModelError("Observaciones", "El motivo de salida es obligatorio");
            }

            // Usamos el mismo ViewModel ya que los campos son idénticos (Id, Cantidad, Observaciones)
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                     return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }
                return RedirectToAction(nameof(Listar)); // Fallback simple para salida si no es ajax
            }

            var inventario = await _inventarioService.GetByIdAsync(viewModel.IdInventario);
            if (inventario == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, errors = new[] { "El insumo no existe." } });
                return RedirectToAction(nameof(Listar));
            }

            if (inventario.StockActual < viewModel.Cantidad)
            {
                var errorMsg = $"Inventario insuficiente (Disponible: {inventario.StockActual})";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, errors = new[] { errorMsg } });
                
                TempData["Error"] = errorMsg;
                return RedirectToAction(nameof(Listar));
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
                 return Json(new { success = false, errors = new[] { "No se pudo registrar la salida." } });
            }

            TempData["Error"] = "No se pudo registrar la salida.";
            return RedirectToAction(nameof(Listar));
        }

        // GET: Inventario/HistorialMovimientos
        public async Task<IActionResult> HistorialMovimientos(int idInventario, DateTime? fechaInicio, DateTime? fechaFin, string? tipoMovimiento)
        {
            var movimientos = await _movimientoService.ListarMovimientosAsync(idInventario, fechaInicio, fechaFin);
            
            if (!string.IsNullOrEmpty(tipoMovimiento))
            {
                movimientos = movimientos.Where(m => m.Tipo_Movimiento != null && m.Tipo_Movimiento.Contains(tipoMovimiento)).ToList();
            }

            ViewBag.IdInventario = idInventario;
            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;
            ViewBag.TipoMovimiento = tipoMovimiento;
            
            return PartialView("_HistorialMovimientosModal", movimientos);
        }

        public async Task<IActionResult> ExportarHistorialPdf(int idInventario, DateTime? fechaInicio, DateTime? fechaFin, string? tipoMovimiento)
        {
            var movimientos = await _movimientoService.ListarMovimientosAsync(idInventario, fechaInicio, fechaFin);
            if (!string.IsNullOrEmpty(tipoMovimiento))
            {
                movimientos = movimientos.Where(m => m.Tipo_Movimiento != null && m.Tipo_Movimiento.Contains(tipoMovimiento)).ToList();
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
                movimientos = movimientos.Where(m => m.Tipo_Movimiento != null && m.Tipo_Movimiento.Contains(tipoMovimiento)).ToList();
            }

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("ID,Tipo,Cantidad,Fecha,Usuario");

            foreach (var mov in movimientos)
            {
                builder.AppendLine($"{mov.Id_Movimiento},{mov.Tipo_Movimiento},{mov.Cantidad},{mov.Fecha},{mov.Nombre_Usuario}");
            }

            var content = builder.ToString();
            var buffer = System.Text.Encoding.UTF8.GetBytes(content);
            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var result = new byte[preamble.Length + buffer.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(buffer, 0, result, preamble.Length, buffer.Length);

            return File(result, "text/csv", $"Historial_{idInventario}_{DateTime.Now:yyyyMMdd}.csv");
        }

        // POST: Inventario/AnularMovimiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,EncargadoInventario")]
        public async Task<IActionResult> AnularMovimiento(int Id_Movimiento, string Motivo)
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
            if (usuarioId == 0) return Json(new { success = false, message = "Sesión expirada" });

            if (string.IsNullOrWhiteSpace(Motivo)) return Json(new { success = false, message = "El motivo de anulación es obligatorio." });

            var result = await _movimientoService.AnularMovimientoAsync(Id_Movimiento, Motivo, usuarioId);

            if (result)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Movimiento anulado correctamente." });
                }
                TempData["Success"] = "Movimiento anulado correctamente.";
                return RedirectToAction(nameof(Listar));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "No se pudo anular el movimiento. Compruebe si hay suficiente stock para la anulación." });
            }
            TempData["Error"] = "No se pudo anular el movimiento.";
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

        [HttpGet]
        public async Task<IActionResult> ObtenerModalInventario(string accion, int? inventarioId = null)
        {
             try
            {
                var model = new InventarioDto();
                ViewBag.AccionModal = accion;

                if (inventarioId.HasValue && (accion == "editar" || accion == "ver"))
                {
                    var inventario = await _inventarioService.GetByIdAsync(inventarioId.Value);
                    if (inventario != null)
                    {
                        model = inventario;
                    }
                }

                return PartialView("_InventarioModal", model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerModalInventario: {ex.Message}");
                ViewBag.AccionModal = accion;
                return PartialView("_InventarioModal", new InventarioDto());
            }
        }
    }
}