using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Response.Factura;
using VentaFacil.web.Models.ViewModel;
using VentaFacil.web.Services.Email;
using VentaFacil.web.Services.Facturacion;
using VentaFacil.web.Services.PDF;
using VentaFacil.web.Services.Pedido;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class FacturacionController : Controller
    {
        private readonly IFacturacionService _facturacionService;
        private readonly IPedidoService _pedidoService;
        private readonly IPdfService _pdfService;
        private readonly Services.BCCR.IBccrService _bccrService;
        private readonly IEmailService _emailService;
        private readonly ILogger<FacturacionController> _logger;
        private readonly Data.ApplicationDbContext _context;

        public FacturacionController(
            IFacturacionService facturacionService,
            IPedidoService pedidoService,
            IPdfService pdfService,
            Services.BCCR.IBccrService bccrService,
            IEmailService emailService,
            ILogger<FacturacionController> logger,
            Data.ApplicationDbContext context)
        {
            _facturacionService = facturacionService;
            _pedidoService = pedidoService;
            _pdfService = pdfService;
            _bccrService = bccrService;
            _emailService = emailService;
            _logger = logger;
            _context = context;
        }

        // GET: /Facturacion
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, int? numeroFactura, string? cliente)
        {
            try
            {
                // Valores por defecto para fechas si no se especifican y no es búsqueda por número
                if (!fechaInicio.HasValue && !numeroFactura.HasValue && string.IsNullOrEmpty(cliente))
                {
                    fechaInicio = DateTime.Today;
                    fechaFin = DateTime.Today;
                }

                var facturas = await _facturacionService.BuscarFacturasAsync(fechaInicio, fechaFin, numeroFactura, cliente);

                ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
                ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
                ViewBag.NumeroFactura = numeroFactura;
                ViewBag.Cliente = cliente;

                return View(facturas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar facturas");
                TempData["Error"] = "Error al cargar el historial de facturas";
                return View(new List<FacturaDto>());
            }
        }

        // GET: /Facturacion/ProcesarPago/{pedidoId}
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

                
                if (!EsEstadoValidoParaFacturacion(pedido.Estado))
                {
                    TempData["Error"] = $"El pedido no está en estado válido para pago. Estado actual: {pedido.Estado}";
                    return RedirectToAction("Editar", "Pedidos", new { id = pedidoId });
                }

                
                if (pedido.Items.Any(i => i.PrecioUnitario <= 0))
                {
                    var productoSinPrecio = pedido.Items.FirstOrDefault(i => i.PrecioUnitario <= 0);
                    TempData["Error"] = $"Error: El producto '{productoSinPrecio?.NombreProducto}' no tiene precio asignado.";
                    return RedirectToAction("Editar", "Pedidos", new { id = pedidoId });
                }

                var model = new ProcesarPagoViewModel
                {
                    PedidoId = pedidoId,
                    Pedido = pedido,
                    Total = pedido.Total,
                    Cliente = pedido.Cliente,
                    Modalidad = pedido.Modalidad,
                    NumeroMesa = pedido.NumeroMesa
                };

                // Obtener Tipo de Cambio
                try 
                {
                    var (compra, venta) = await _bccrService.ObtenerTipoDeCambioDelDiaAsync();
                    ViewBag.TipoCambioCompra = compra;
                    ViewBag.TipoCambioVenta = venta;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener tipo de cambio del BCCR");
                    // Valores por defecto o 0 si falla
                    ViewBag.TipoCambioCompra = 0;
                    ViewBag.TipoCambioVenta = 0;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar pago para pedido {PedidoId}", pedidoId);
                TempData["Error"] = $"Error al procesar pago: {ex.Message}";
                return RedirectToAction("Index", "Pedidos");
            }
        }

        // POST: /Facturacion/ProcesarPago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(ProcesarPagoViewModel model)
        {
            try
            {
                // Si es pago mixto, validar la lista de pagos
                if (model.MetodoPago == MetodoPago.Mixto)
                {
                    // Limpiar errores de validación de campos individuales si es mixto
                    ModelState.Remove("MontoPagado");
                    ModelState.Remove("TasaCambio");

                    if (model.Pagos == null || !model.Pagos.Any())
                    {
                        ModelState.AddModelError("Pagos", "Debe agregar al menos un método de pago.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    await CargarDatosPedidoEnModelo(model);
                    return View(model);
                }

                // Validar estado del pedido antes de procesar
                var pedido = await _pedidoService.ObtenerPedidoAsync(model.PedidoId);
                if (!EsEstadoValidoParaFacturacion(pedido.Estado))
                {
                    TempData["Warning"] = $"Este pedido ya fue procesado. Estado actual: {pedido.Estado}";
                    return RedirectToAction("Index", "Pedidos");
                }

                if (pedido.Items.Any(i => i.PrecioUnitario <= 0))
                {
                    var productoSinPrecio = pedido.Items.FirstOrDefault(i => i.PrecioUnitario <= 0);
                    TempData["Error"] = $"Error: El producto '{productoSinPrecio?.NombreProducto}' no tiene precio asignado.";
                    return RedirectToAction("Editar", "Pedidos", new { id = model.PedidoId });
                }

                ResultadoFacturacion resultadoFacturacion;

                if (model.MetodoPago == MetodoPago.Mixto)
                {
                     // Validar que la suma de pagos cubra el total
                     decimal totalPagado = 0;
                     foreach(var pago in model.Pagos)
                     {
                         if (pago.Moneda == "USD")
                         {
                             if (!pago.TasaCambio.HasValue || pago.TasaCambio <= 0)
                             {
                                 ModelState.AddModelError("", "Tasa de cambio inválida para pago en USD");
                                 await CargarDatosPedidoEnModelo(model);
                                 return View(model);
                             }
                             totalPagado += pago.Monto * pago.TasaCambio.Value;
                         }
                         else
                         {
                             totalPagado += pago.Monto;
                         }
                     }

                     if (!model.EsPagoParcial && totalPagado < pedido.Total - 0.01m)
                     {
                         ModelState.AddModelError("", $"El monto total pagado ({totalPagado:C}) es menor al total del pedido ({pedido.Total:C})");
                         await CargarDatosPedidoEnModelo(model);
                         return View(model);
                     }

                     resultadoFacturacion = await _facturacionService.GenerarFacturaMixtaAsync(model.PedidoId, model.Pagos, model.EsPagoParcial);
                }
                else
                {
                    // Validar monto de pago único
                    var resultadoValidacion = _facturacionService.ValidarMontoPago(pedido.Total, model.MontoPagado, model.Moneda, model.TasaCambio, model.EsPagoParcial);
                    if (!resultadoValidacion.EsValido)
                    {
                        ModelState.AddModelError("MontoPagado", resultadoValidacion.Mensaje);
                        await CargarDatosPedidoEnModelo(model);
                        return View(model);
                    }

                    // Generar factura única
                    resultadoFacturacion = await GenerarFacturaAsync(model, pedido.Total);
                }

                if (resultadoFacturacion.Success)
                {
                    // Enviar pedido a cocina automáticamente después de facturar
                    await _pedidoService.IniciarPreparacionAsync(model.PedidoId);

                    TempData["Success"] = "✅ Pago procesado exitosamente. El pedido ha sido enviado a cocina.";
                    return RedirectToAction("DetalleFactura", new { facturaId = resultadoFacturacion.FacturaId });
                }
                else
                {
                    TempData["Error"] = resultadoFacturacion.Message ?? "Error al procesar el pago";
                    await CargarDatosPedidoEnModelo(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar pago para pedido {PedidoId}", model.PedidoId);
                TempData["Error"] = $"❌ Error al procesar pago: {ex.Message}";
                await CargarDatosPedidoEnModelo(model);
                return View(model);
            }
        }

        // GET: /Facturacion/DetalleFactura/{facturaId}
        [HttpGet]
        public async Task<IActionResult> DetalleFactura(int facturaId)
        {
            try
            {
                var factura = await _facturacionService.ObtenerFacturaAsync(facturaId);

                if (factura == null)
                {
                    _logger.LogWarning("Factura {FacturaId} no encontrada", facturaId);
                    TempData["Error"] = "Factura no encontrada";
                    return RedirectToAction("Index", "Pedidos");
                }

                var viewModel = new DetalleFacturaViewModel
                {
                    Factura = factura,
                    FacturaId = facturaId
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar factura {FacturaId}", facturaId);
                TempData["Error"] = $"Error al cargar la factura: {ex.Message}";
                return RedirectToAction("Index", "Pedidos");
            }
        }


        [HttpGet]
        public async Task<IActionResult> DescargarFactura([FromQuery] int facturaId, [FromQuery] bool esCopia = false)
        {
            try
            {
                // Validar que el ID sea válido
                if (facturaId <= 0)
                {
                    _logger.LogWarning("ID de factura inválido: {FacturaId}", facturaId);
                    return BadRequest("ID de factura inválido");
                }

                _logger.LogInformation("Solicitando descarga de factura ID: {FacturaId}, Es Copia: {EsCopia}", facturaId, esCopia);

                // Si es copia, siempre generar on-demand para incluir la marca de agua
                if (!esCopia)
                {
                    // Intentar obtener PDF desde la base de datos
                    var factura = await _context.Factura
                        .Where(f => f.Id_Factura == facturaId)
                        .Select(f => new { f.PdfData, f.PdfFileName })
                        .FirstOrDefaultAsync();

                    if (factura?.PdfData != null)
                    {
                        _logger.LogInformation("PDF encontrado en BD para factura {FacturaId}", facturaId);
                        return File(factura.PdfData, "application/pdf",
                            factura.PdfFileName ?? $"factura-{facturaId}.pdf");
                    }
                }

                // Generar PDF on-demand si no existe en BD o es copia
                _logger.LogWarning("Generando PDF on-demand para factura {FacturaId}. Es Copia: {EsCopia}", facturaId, esCopia);
                var facturaDto = await _facturacionService.ObtenerFacturaAsync(facturaId);

                if (facturaDto == null)
                {
                    _logger.LogWarning("Factura {FacturaId} no encontrada en el servicio", facturaId);
                    return NotFound("Factura no encontrada");
                }

                var pdfBytes = _pdfService.GenerarFacturaPdf(facturaDto, esCopia);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogError("PDF generado vacío para factura {FacturaId}", facturaId);
                    return BadRequest("Error al generar el PDF");
                }

                _logger.LogInformation("PDF generado exitosamente para factura {FacturaId}", facturaId);
                return File(pdfBytes, "application/pdf", $"factura-{facturaId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al obtener PDF para factura {FacturaId}", facturaId);
                return StatusCode(500, $"Error interno al generar PDF: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int facturaId, string justificacion)
        {
            try
            {
                await _facturacionService.AnularFacturaAsync(facturaId, justificacion);
                TempData["Success"] = "Factura anulada correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al anular factura {FacturaId}", facturaId);
                TempData["Error"] = $"Error al anular la factura: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPorEmail(int facturaId, string emailDestino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(emailDestino))
                    return Json(new { success = false, message = "El correo es requerido." });

                var facturaDto = await _facturacionService.ObtenerFacturaAsync(facturaId);
                if (facturaDto == null)
                    return Json(new { success = false, message = "Factura no encontrada." });

                // Obtener PDF (de BD o generar on-demand)
                byte[] pdfBytes;
                var facturaEntity = await _context.Factura
                    .Where(f => f.Id_Factura == facturaId)
                    .Select(f => new { f.PdfData })
                    .FirstOrDefaultAsync();

                if (facturaEntity?.PdfData != null)
                {
                    pdfBytes = facturaEntity.PdfData;
                }
                else
                {
                    pdfBytes = _pdfService.GenerarFacturaPdf(facturaDto);
                }

                // Construir cuerpo del email en HTML
                var asunto = $"VentaFácil - Factura #{facturaDto.NumeroFactura}";
                var cuerpo = $"""
                    <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;">
                        <div style="background:#4f46e5;padding:24px;border-radius:12px 12px 0 0;text-align:center;">
                            <h1 style="color:#fff;margin:0;font-size:1.5rem;">VentaFácil</h1>
                            <p style="color:#c7d2fe;margin:4px 0 0;">Comprobante de Pago</p>
                        </div>
                        <div style="background:#f8fafc;padding:24px;border-radius:0 0 12px 12px;border:1px solid #e2e8f0;">
                            <p style="color:#334155;">Estimado cliente,</p>
                            <p style="color:#334155;">Adjunto encontrará su factura <strong>{facturaDto.NumeroFactura}</strong> emitida el <strong>{facturaDto.FechaEmision:dd/MM/yyyy}</strong>.</p>
                            <table style="width:100%;border-collapse:collapse;margin:16px 0;">
                                <tr><td style="padding:8px;color:#64748b;">Total:</td><td style="padding:8px;font-weight:bold;color:#1e293b;">{facturaDto.Total:C}</td></tr>
                                <tr><td style="padding:8px;color:#64748b;">Método de Pago:</td><td style="padding:8px;color:#1e293b;">{facturaDto.MetodoPago}</td></tr>
                            </table>
                            <p style="color:#64748b;font-size:0.85rem;">Este es un mensaje automático generado por VentaFácil. Por favor no responda este correo.</p>
                        </div>
                    </div>
                """;

                // Enviar usando el servicio de email
                // Como IEmailService.SendEmailAsync solo soporta body HTML (sin adjuntos),
                // incluimos un enlace embebido y el PDF codificado en base64 en el cuerpo.
                var pdfBase64 = Convert.ToBase64String(pdfBytes);
                var cuerpoConPdf = cuerpo + $"""
                    <div style="margin-top:16px;text-align:center;">
                        <a href="data:application/pdf;base64,{pdfBase64}" download="Factura-{facturaId}.pdf"
                           style="background:#4f46e5;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:bold;">
                            ⬇ Descargar Factura PDF
                        </a>
                    </div>
                """;

                await _emailService.SendEmailAsync(emailDestino, asunto, cuerpoConPdf);

                _logger.LogInformation("Factura {FacturaId} enviada por email a {Email}", facturaId, emailDestino);
                return Json(new { success = true, message = $"Factura enviada exitosamente a {emailDestino}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar factura {FacturaId} por email a {Email}", facturaId, emailDestino);
                return Json(new { success = false, message = $"Error al enviar el correo: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Devolucion(int facturaId, List<int> itemsDevolver)
        {
            try
            {
                if (itemsDevolver == null || !itemsDevolver.Any())
                {
                    TempData["Error"] = "Debe seleccionar al menos un producto para devolver.";
                    return RedirectToAction("DetalleFactura", new { facturaId });
                }

                var notaCreditoId = await _facturacionService.GenerarNotaCreditoAsync(facturaId, itemsDevolver);
                TempData["Success"] = $"Nota de Crédito generada exitosamente (ID: {notaCreditoId}).";
                return RedirectToAction("DetalleFactura", new { facturaId = notaCreditoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar devolución para factura {FacturaId}", facturaId);
                TempData["Error"] = $"Error al procesar devolución: {ex.Message}";
                return RedirectToAction("DetalleFactura", new { facturaId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAbono(int facturaId, decimal montoAbono, string metodoPagoAbono)
        {
            try
            {
                await _facturacionService.RegistrarAbonoAsync(facturaId, montoAbono, metodoPagoAbono);
                TempData["Success"] = "Abono registrado exitosamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar abono en factura {FacturaId}", facturaId);
                TempData["Error"] = $"Error al registrar abono: {ex.Message}";
            }
            return RedirectToAction("Index");
        }


        #region Métodos Privados

        private bool EsEstadoValidoParaFacturacion(PedidoEstado estado)
        {
            return estado == PedidoEstado.Pendiente || estado == PedidoEstado.Borrador;
        }



        private async Task<ResultadoFacturacion> GenerarFacturaAsync(ProcesarPagoViewModel model, decimal totalPedido)
        {
            if (model.Moneda == "USD")
            {
                return await _facturacionService.GenerarFacturaDolaresAsync(
                    model.PedidoId,
                    model.MontoPagado,
                    model.TasaCambio.Value,
                    model.EsPagoParcial);
            }
            else
            {
                return await _facturacionService.GenerarFacturaAsync(
                    model.PedidoId,
                    model.MetodoPago,
                    model.MontoPagado,
                    model.Moneda,
                    model.EsPagoParcial);
            }
        }

        private async Task CargarDatosPedidoEnModelo(ProcesarPagoViewModel model)
        {
            if (model.Pedido == null)
            {
                model.Pedido = await _pedidoService.ObtenerPedidoAsync(model.PedidoId);
                model.Total = model.Pedido?.Total ?? 0;
                model.Cliente = model.Pedido?.Cliente;
            }
        }

        #endregion
    }

    #region ViewModels

    public class ProcesarPagoViewModel
    {
        public int PedidoId { get; set; }
        public PedidoDto? Pedido { get; set; }
        public decimal Total { get; set; }
        public string Cliente { get; set; }
        public ModalidadPedido Modalidad { get; set; }
        public int? NumeroMesa { get; set; }
        
        public bool EsPagoParcial { get; set; }

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

        public List<PagoFacturaDto> Pagos { get; set; } = new List<PagoFacturaDto>();

        public decimal Cambio => Math.Max(0, MontoPagado - TotalEnMonedaPago);
        public decimal TotalEnMonedaPago => Moneda == "USD" ? (Total / (TasaCambio ?? 1)) : Total;
        public bool MostrarTasaCambio => Moneda == "USD";
    }

    #endregion
}