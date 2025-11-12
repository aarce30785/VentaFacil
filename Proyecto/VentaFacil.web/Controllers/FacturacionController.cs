using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Enums;
using VentaFacil.web.Models.Response.Factura;
using VentaFacil.web.Services.Facturacion;
using VentaFacil.web.Services.Pedido;

namespace VentaFacil.web.Controllers
{
    public class FacturacionController : Controller
    {
        private readonly IFacturacionService _facturacionService;
        private readonly IPedidoService _pedidoService;
        private readonly ILogger<FacturacionController> _logger;

        public FacturacionController(
            IFacturacionService facturacionService,
            IPedidoService pedidoService,
            ILogger<FacturacionController> logger)
        {
            _facturacionService = facturacionService;
            _pedidoService = pedidoService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ProcesarPago(int pedidoId)
        {
            try
            {
                Console.WriteLine($"=== PROCESAR PAGO FACTURACION ===");
                Console.WriteLine($"Pedido ID: {pedidoId}");

               
                var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);

                Console.WriteLine($"Pedido obtenido: {pedido != null}");
                Console.WriteLine($"Estado del pedido: {pedido?.Estado}");
                Console.WriteLine($"Items count: {pedido?.Items?.Count}");

                if (pedido == null)
                {
                    TempData["Error"] = "Pedido no encontrado";
                    return RedirectToAction("Index", "Pedidos");
                }

                if (pedido.Estado != PedidoEstado.Pendiente && pedido.Estado != PedidoEstado.Listo)
                {
                    TempData["Error"] = $"El pedido no está en estado válido para pago. Estado actual: {pedido.Estado}";
                    Console.WriteLine($"ERROR: Estado inválido - {pedido.Estado}");
                    return RedirectToAction("Editar", "Pedidos", new { id = pedidoId });
                }

                
                var model = new ProcesarPagoViewModel
                {
                    Pedido = pedido,
                    PedidoId = pedidoId,
                    Total = pedido.Total,
                    Cliente = pedido.Cliente,
                    
                };

                Console.WriteLine("Redirigiendo a vista ProcesarPago con modelo");
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR en ProcesarPago: {ex.Message}");
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
                Console.WriteLine($"=== CONFIRMAR PAGO ===");
                Console.WriteLine($"Pedido ID: {model.PedidoId}");
                Console.WriteLine($"Método Pago: {model.MetodoPago}");
                Console.WriteLine($"Monto Pagado: {model.MontoPagado}");
                Console.WriteLine($"Moneda: {model.Moneda}");
                Console.WriteLine($"Tasa Cambio: {model.TasaCambio}");

               
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState inválido");

                   
                    var pedido = await _pedidoService.ObtenerPedidoAsync(model.PedidoId);
                    model.Pedido = pedido;
                    return View("ProcesarPago", model);
                }

                
                var pedidoValidacion = await _pedidoService.ObtenerPedidoAsync(model.PedidoId);
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

                // Procesar el pago
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

                Console.WriteLine($"Resultado facturación - Success: {resultado?.Success}, Message: {resultado?.Message}");

                if (resultado?.Success == true)
                {
                    Console.WriteLine("Pago exitoso - Enviando a cocina");

                    
                    var resultadoCocina = await _pedidoService.IniciarPreparacionAsync(model.PedidoId);

                    Console.WriteLine($"Resultado cocina - Success: {resultadoCocina?.Success}, Message: {resultadoCocina?.Message}");

                    if (resultadoCocina?.Success == true)
                    {
                        TempData["Success"] = "✅ Pago procesado exitosamente. El pedido ha sido enviado a cocina.";

                        
                        return RedirectToAction("Index", "Pedidos");
                    }
                    else
                    {
                        TempData["Warning"] = $"⚠️ Pago procesado pero error al enviar a cocina: {resultadoCocina?.Message}";

                       
                        return RedirectToAction("Index", "Pedidos");
                    }
                }
                else
                {
                    Console.WriteLine($"Error en facturación: {resultado?.Message}");

                    
                    model.Pedido = pedidoValidacion;
                    TempData["Error"] = resultado?.Message ?? "Error desconocido al procesar el pago";

                    if (resultado?.Errores?.Any() == true)
                    {
                        TempData["ErrorDetalles"] = string.Join("\n", resultado.Errores);
                    }

                    return View("ProcesarPago", model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR en ProcesarPago: {ex.Message}");

                
                var pedido = await _pedidoService.ObtenerPedidoAsync(model.PedidoId);
                model.Pedido = pedido;
                TempData["Error"] = $"❌ Error al procesar pago: {ex.Message}";

                return View("ProcesarPago", model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> DetalleFactura(int facturaId)
        {
            try
            {
                var factura = await _facturacionService.ObtenerFacturaAsync(facturaId);

                if (factura == null)
                {
                    TempData["Error"] = "Factura no encontrada";
                    return RedirectToAction("Index", "Pedidos"); 
                }

                return View(factura);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener factura {FacturaId}", facturaId);
                TempData["Error"] = "Error al cargar la factura";
                return RedirectToAction("Index", "Pedidos"); 
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImprimirFactura(int facturaId)
        {
            try
            {
                var factura = await _facturacionService.ObtenerFacturaAsync(facturaId);

                if (factura == null)
                {
                    return Json(new { success = false, message = "Factura no encontrada" });
                }

                
                _logger.LogInformation("Imprimiendo factura {NumeroFactura}", factura.NumeroFactura);

                return Json(new { success = true, message = "Factura enviada a impresión" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al imprimir factura {FacturaId}", facturaId);
                return Json(new { success = false, message = "Error al imprimir factura" });
            }
        }
    }

    public class ProcesarPagoViewModel
    {
        public int PedidoId { get; set; }
        public PedidoDto? Pedido { get; set; }
        public decimal Total { get; set; }

        [Required(ErrorMessage = "El método de pago es requerido")]
        public MetodoPago MetodoPago { get; set; }

        [Required(ErrorMessage = "El monto pagado es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal MontoPagado { get; set; }

        public string Moneda { get; set; } = "CRC";

        [Range(0.01, double.MaxValue, ErrorMessage = "La tasa de cambio debe ser mayor a 0")]
        public decimal? TasaCambio { get; set; }

        public decimal Cambio => MontoPagado - (Moneda == "USD" ? (Total / (TasaCambio ?? 1)) : Total);
        public string Cliente { get; set; }
        public ModalidadPedido Modalidad { get; set; }
        public int? NumeroMesa { get; set; }
    }

}
