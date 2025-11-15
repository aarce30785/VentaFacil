using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Enums;
using VentaFacil.web.Models.Response.Factura;
using VentaFacil.web.Services.Facturacion;
using VentaFacil.web.Services.PDF;
using VentaFacil.web.Services.Pedido;

namespace VentaFacil.web.Controllers
{
    public class FacturacionController : Controller
    {
        private readonly IFacturacionService _facturacionService;
        private readonly IPedidoService _pedidoService;
        private readonly IPdfService _pdfService; 
        private readonly ILogger<FacturacionController> _logger;

        public FacturacionController(
            IFacturacionService facturacionService,
            IPedidoService pedidoService,
            IPdfService pdfService, 
            ILogger<FacturacionController> logger)
        {
            _facturacionService = facturacionService;
            _pedidoService = pedidoService;
            _pdfService = pdfService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ProcesarPago(int pedidoId)
        {
            try
            {
                var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);

                if (pedido == null)
                {
                    TempData["Error"] = "Pedido no encontrado";
                    return RedirectToAction("Index", "Pedidos");
                }

                if (pedido.Estado != PedidoEstado.Pendiente && pedido.Estado != PedidoEstado.Listo)
                {
                    TempData["Error"] = $"El pedido no está en estado válido para pago. Estado actual: {pedido.Estado}";
                    return RedirectToAction("Editar", "Pedidos", new { id = pedidoId });
                }

                var model = new ProcesarPagoViewModel
                {
                    Pedido = pedido,
                    PedidoId = pedidoId,
                    Total = pedido.Total,
                    Cliente = pedido.Cliente,
                };
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar pago: {ex.Message}";
                return RedirectToAction("Index", "Pedidos");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(ProcesarPagoViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var pedido = await _pedidoService.ObtenerPedidoAsync(model.PedidoId);
                    model.Pedido = pedido;
                    return View("ProcesarPago", model);
                }

                var pedidoValidacion = await _pedidoService.ObtenerPedidoAsync(model.PedidoId);
                if (pedidoValidacion.Estado != PedidoEstado.Pendiente && pedidoValidacion.Estado != PedidoEstado.Listo)
                {
                    TempData["Warning"] = $"Este pedido ya fue procesado. Estado actual: {pedidoValidacion.Estado}";
                    return RedirectToAction("Index", "Pedidos");
                }

                decimal totalPedido = pedidoValidacion.Total;

                if (model.Moneda == "USD")
                {
                    if (!model.TasaCambio.HasValue || model.TasaCambio <= 0)
                    {
                        ModelState.AddModelError("TasaCambio", "La tasa de cambio es requerida para pagos en dólares");
                        model.Pedido = pedidoValidacion;
                        return View("ProcesarPago", model);
                    }
                    totalPedido = totalPedido / model.TasaCambio.Value;
                }

                if (model.MontoPagado < totalPedido)
                {
                    ModelState.AddModelError("MontoPagado", $"El monto pagado ({model.MontoPagado}) es menor al total del pedido ({totalPedido})");
                    model.Pedido = pedidoValidacion;
                    return View("ProcesarPago", model);
                }

      
                ResultadoFacturacion resultado;

                if (model.Moneda == "USD")
                {
                    resultado = await _facturacionService.GenerarFacturaDolaresAsync(
                        model.PedidoId,
                        model.MontoPagado,
                        model.TasaCambio.Value);
                }
                else
                {
                    resultado = await _facturacionService.GenerarFacturaAsync(
                        model.PedidoId,
                        model.MetodoPago,
                        model.MontoPagado,
                        model.Moneda);
                }

                bool esExitoso = resultado?.Success == true && resultado.FacturaId > 0;

                if (esExitoso)
                {

                    var resultadoCocina = await _pedidoService.IniciarPreparacionAsync(model.PedidoId);

                    if (resultadoCocina?.Success == true)
                    {
                        TempData["Success"] = "✅ Pago procesado exitosamente. El pedido ha sido enviado a cocina.";
                    }
                    else
                    {
                        TempData["Warning"] = $"⚠️ Pago procesado pero error al enviar a cocina: {resultadoCocina?.Message}";
                    }

                    
                    return RedirectToAction("DetalleFactura", new { facturaId = resultado.FacturaId });
                }
                else
                {
                    
                    TempData["Error"] = resultado?.Message ?? "Error desconocido al procesar el pago";
                    return View("ProcesarPago", model);
                }
            }
            catch (Exception ex)
            {
                
                var pedido = await _pedidoService.ObtenerPedidoAsync(model.PedidoId);
                model.Pedido = pedido;
                TempData["Error"] = $"❌ Error al procesar pago: {ex.Message}";
                return View("ProcesarPago", model);
            }
        }

        [HttpGet]
        public IActionResult PagoCompletado(int facturaId)
        {
            ViewBag.FacturaId = facturaId;
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> DetalleFactura(int facturaId)
        {
            try
            {
                

                var factura = await _facturacionService.ObtenerFacturaAsync(facturaId);

                if (factura == null)
                {
                    _logger.LogWarning($"❌ Factura {facturaId} no encontrada");
                    TempData["Error"] = "Factura no encontrada";
                    return RedirectToAction("Index", "Pedidos");
                }

                

                var viewModel = new DetalleFacturaViewModel
                {
                    Factura = factura,
                    FacturaId = facturaId
                };
                var pdfBytes = _pdfService.GenerarFacturaPdf(factura);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"💥 Error crítico al cargar factura {facturaId}");
                TempData["Error"] = $"Error al cargar la factura: {ex.Message}";
                return RedirectToAction("Index", "Pedidos");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerarPdf([FromBody] GenerarPdfRequest request)
        {
            try
            {

                var factura = await _facturacionService.ObtenerFacturaAsync(request.FacturaId);

                if (factura == null)
                {
                    return NotFound(new { error = "Factura no encontrada" });
                }

                var pdfBytes = _pdfService.GenerarFacturaPdf(factura);

                return File(pdfBytes, "application/pdf", $"factura-{request.FacturaId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF para factura {FacturaId}", request.FacturaId);
                return BadRequest(new { error = ex.Message });
            }
        }

        public class GenerarPdfRequest
        {
            public int FacturaId { get; set; }
        }

        // ViewModel para DetalleFactura
        public class DetalleFacturaViewModel
        {
            public FacturaDto Factura { get; set; }
            public int FacturaId { get; set; }
        }

    }

    public class ProcesarPagoViewModel
    {
        public int PedidoId { get; set; }
        public PedidoDto? Pedido { get; set; }
        public decimal Total { get; set; }

        [Required(ErrorMessage = "El método de pago es requerido")]
        [Display(Name = "Método de Pago")]
        public MetodoPago MetodoPago { get; set; }

        [Required(ErrorMessage = "El monto pagado es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        [Display(Name = "Monto Pagado")]
        public decimal MontoPagado { get; set; }

        [Display(Name = "Moneda")]
        public string Moneda { get; set; } = "CRC";

        [Range(0.01, double.MaxValue, ErrorMessage = "La tasa de cambio debe ser mayor a 0")]
        [Display(Name = "Tasa de Cambio")]
        public decimal? TasaCambio { get; set; }

        [Display(Name = "Cambio")]
        public decimal Cambio => Math.Max(0, MontoPagado - (Moneda == "USD" ? (Total / (TasaCambio ?? 1)) : Total));

        public string Cliente { get; set; }

        [Display(Name = "Modalidad")]
        public ModalidadPedido Modalidad { get; set; }

        [Display(Name = "Número de Mesa")]
        public int? NumeroMesa { get; set; }

        public decimal TotalEnMonedaPago => Moneda == "USD" ? (Total / (TasaCambio ?? 1)) : Total;
        public bool MostrarTasaCambio => Moneda == "USD";
    }

    public class DetalleFacturaViewModel
    {
        public FacturaDto Factura { get; set; } = new FacturaDto();
        public int FacturaId { get; set; }

        public string NumeroFactura => Factura?.NumeroFactura ?? "N/A";
        public DateTime FechaEmision => Factura?.FechaEmision ?? DateTime.Now;
        public string Cliente => Factura?.Cliente ?? "N/A";
        public decimal Total => Factura?.Total ?? 0;
        public decimal MontoPagado => Factura?.MontoPagado ?? 0;
        public decimal Cambio => Factura?.Cambio ?? 0;
        public string Moneda => Factura?.Moneda ?? "CRC";
        public string MetodoPago => Factura?.MetodoPago.ToString() ?? "N/A";

        public IEnumerable<ItemFacturaDto> Items => Factura?.Items ?? Enumerable.Empty<ItemFacturaDto>();
    }
}