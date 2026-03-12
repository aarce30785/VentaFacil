using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VentaFacil.web.Services.Caja;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using System.Threading.Tasks;
using System.Linq;

using VentaFacil.web.Services.PDF;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class CajaController : Controller
    {
        private readonly ICajaService _cajaService;
        private readonly IPdfService _pdfService;

        public CajaController(ICajaService cajaService, IPdfService pdfService)
        {
            _cajaService = cajaService;
            _pdfService = pdfService;
        }

        // Acción para listar todas las cajas
        public async Task<IActionResult> Listar()
        {
            var cajas = await _cajaService.ListarCajasAsync();
            var cajasDto = cajas.Select(c => new CajaDto
            {
                Id_Caja = c.Id_Caja,
                Id_Usuario = c.Id_Usuario,
                Fecha_Apertura = c.Fecha_Apertura,
                Fecha_Cierre = c.Fecha_Cierre,
                Monto_Inicial = c.Monto_Inicial,
                Monto = c.Monto,
                Monto_Inicial_USD = c.Monto_Inicial_USD,
                Monto_USD = c.Monto_USD,
                Estado = c.Estado
                
            }).ToList();

            return View(cajasDto); 
        }

        // GET: CajaController/Abrir
        public IActionResult Abrir()
        {
            return View();
        }

        // POST: CajaController/Abrir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Abrir(decimal montoInicial, decimal montoInicialUSD)
        {
            if (montoInicial < 0 || montoInicialUSD < 0)
            {
                TempData["Error"] = "Los montos de apertura no pueden ser negativos.";
                return RedirectToAction(nameof(Listar));
            }

            if (ModelState.IsValid)
            {
                int idUsuario = 1; //Obtener el ID del usuario autenticado
                await _cajaService.AbrirCajaAsync(idUsuario, montoInicial, montoInicialUSD);
                TempData["Success"] = "Caja abierta correctamente.";
                return RedirectToAction(nameof(Listar));
            }
            
            return RedirectToAction(nameof(Listar));
        }

        // GET: CajaController/Cerrar/5
        public IActionResult Cerrar(int id)
        {
            ViewBag.IdCaja = id;
            return View();
        }

        // POST: CajaController/Cerrar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarConfirmado(int id, decimal montoFisico, decimal montoFisicoUSD, string justificacion = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    int idUsuario = 1; // Obtener el ID del usuario autenticado
                    await _cajaService.CerrarCajaAsync(id, idUsuario, montoFisico, montoFisicoUSD, justificacion);
                    TempData["Success"] = "Caja cerrada correctamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                }
                return RedirectToAction(nameof(Listar));
            }
            return RedirectToAction(nameof(Listar));
        }

        // GET: CajaController/Retiro/5
        public IActionResult Retiro(int id)
        {
            ViewBag.IdCaja = id;
            return View();
        }

        // POST: CajaController/Retiro/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retiro(int id, decimal monto, string motivo)
        {
            if (monto <= 0)
            {
                TempData["Error"] = "El monto a retirar debe ser mayor a 0.";
                return RedirectToAction(nameof(Listar));
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["Error"] = "La justificación o motivo es obligatoria para registrar el movimiento.";
                return RedirectToAction(nameof(Listar));
            }

            if (ModelState.IsValid)
            {
                int idUsuario = 1; //Obtener el ID del usuario autenticado

                await _cajaService.RegistrarRetiroAsync(id, idUsuario, monto, motivo);
                TempData["Success"] = "Retiro registrado correctamente.";
                return RedirectToAction(nameof(Listar));
            }
            
            TempData["Error"] = "Error de validación al intentar registrar el retiro.";
            return RedirectToAction(nameof(Listar));
        }

        // GET: CajaController/Retiros/5
        public async Task<IActionResult> Retiros(int id, DateTime? fechaInicio = null, DateTime? fechaFin = null, string tipoMovimiento = "Todos")
        {
            var retiros = await _cajaService.ObtenerRetirosPorCajaAsync(id);

            ViewBag.IdCaja = id;

            if (fechaInicio.HasValue)
            {
                retiros = retiros.Where(r => r.FechaHora.Date >= fechaInicio.Value.Date).ToList();
            }
            if (fechaFin.HasValue)
            {
                retiros = retiros.Where(r => r.FechaHora.Date <= fechaFin.Value.Date).ToList();
            }

            if (tipoMovimiento == "Salida")
            {
                retiros = retiros.Where(r => r.Monto < 0).ToList();
            }
            else if (tipoMovimiento == "Ingreso")
            {
                retiros = retiros.Where(r => r.Monto > 0).ToList();
            }

            ViewBag.TotalFiltrado = retiros.Sum(r => r.Monto);
            ViewBag.TipoMovimiento = tipoMovimiento;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            // Añadir nombres de usuario a ViewBag o Dto requeriría join, pero para simular lo que había:
            return PartialView("_RetirosCajaPartial", retiros);
        }

        // GET: CajaController/GenerarArqueo
        public async Task<IActionResult> GenerarArqueo(int idCaja)
        {
            var cajas = await _cajaService.ListarCajasAsync();
            var caja = cajas.FirstOrDefault(c => c.Id_Caja == idCaja);
            
            if (caja == null)
            {
                TempData["Error"] = "La caja seleccionada no existe.";
                return RedirectToAction(nameof(Listar));
            }

            var retiros = await _cajaService.ObtenerRetirosPorCajaAsync(idCaja);

            byte[] pdfBytes = _pdfService.GenerarArqueoPdf(caja, retiros);

            return File(pdfBytes, "application/pdf", $"ArqueoCaja_{caja.Fecha_Apertura:yyyyMMdd_HHmm}.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> EstadoCaja()
        {
            var existe = await _cajaService.ExisteCajaAbiertaAsync();
            return Json(new { existe });
        }
    }
}
