using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Response.Factura;
using VentaFacil.web.Services.PDF;
using VentaFacil.web.Services.Pedido;
using VentaFacil.web.Services.Caja;

namespace VentaFacil.web.Services.Facturacion
{
    public class FacturacionService : IFacturacionService
    {
        private readonly IPedidoService _pedidoService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FacturacionService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPdfService _pdfService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICajaService _cajaService;

        public FacturacionService(
            IPedidoService pedidoService,
            ApplicationDbContext context,
            ILogger<FacturacionService> logger,
            IHttpContextAccessor httpContextAccessor,
            IPdfService pdfService,
            IServiceProvider serviceProvider,
            ICajaService cajaService) 
        {
            _pedidoService = pedidoService;
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _pdfService = pdfService;
            _serviceProvider = serviceProvider;
            _cajaService = cajaService;
        }

        public async Task<ResultadoFacturacion> GenerarFacturaAsync(int pedidoId, MetodoPago metodoPago, decimal montoPagado, string moneda = "CRC")
        {
            try
            {
                _logger.LogInformation("🧾 Iniciando generación de factura para pedido {PedidoId}", pedidoId);

                var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);
                var errores = await ValidarPedidoParaFacturacionAsync(pedido);

                if (errores.Any())
                {
                    return ResultadoFacturacion.Error("No se puede generar la factura", errores);
                }

                
                var ventaId = await CrearVentaSimpleAsync(pedido, metodoPago);
                await CrearDetallesVentaAsync(ventaId, pedido);
                var factura = await CrearFacturaAsync(ventaId, pedido, montoPagado, moneda, metodoPago);

                
                var facturaDto = await ObtenerFacturaCompletaAsync(factura.Id_Factura);

                // --- AGREGAR MOVIMIENTOS DE CAJA ---
                if (metodoPago == MetodoPago.Efectivo)
                {
                    // Obtener la caja abierta (sin filtrar por usuario)
                    var caja = await _context.Caja.FirstOrDefaultAsync(c => c.Estado == "Abierta");
                    if (caja != null)
                    {
                        // Registrar ingreso del monto pagado (positivo)
                        await _cajaService.RegistrarRetiroAsync(caja.Id_Caja, caja.Id_Usuario, montoPagado, "Ingreso por venta en efectivo");
                        // Registrar retiro del vuelto si aplica (negativo)
                        var vuelto = montoPagado - factura.Total;
                        if (vuelto > 0)
                        {
                            await _cajaService.RegistrarRetiroAsync(caja.Id_Caja, caja.Id_Usuario, -vuelto, "Retiro de vuelto entregado al cliente");
                        }
                    }
                }
                // --- FIN MOVIMIENTOS DE CAJA ---

                
                _logger.LogInformation("🔍 Factura generada - ID en entidad: {IdEntidad}, ID en DTO: {IdDto}",
                    factura.Id_Factura, facturaDto?.Id_Factura);

                
                _ = Task.Run(() => GenerarYGuardarPdfAsync(factura.Id_Factura));

                _logger.LogInformation("✅ Factura {FacturaId} generada exitosamente para pedido {PedidoId}",
                    factura.Id_Factura, pedidoId);

                
                return ResultadoFacturacion.Exitoso(facturaDto, "Factura generada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error al generar factura para pedido {PedidoId}", pedidoId);
                return ResultadoFacturacion.Error($"Error al generar factura: {ex.Message}");
            }
        }

        public async Task<ResultadoFacturacion> GenerarFacturaDolaresAsync(int pedidoId, decimal montoPagado, decimal tasaCambio)
        {
            try
            {
                _logger.LogInformation("💵 Generando factura en dólares para pedido {PedidoId}", pedidoId);

                var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);
                var errores = await ValidarPedidoParaFacturacionAsync(pedido);

                if (errores.Any())
                {
                    return ResultadoFacturacion.Error("No se puede generar la factura", errores);
                }

                
                var ventaId = await CrearVentaSimpleAsync(pedido, MetodoPago.Efectivo);
                await CrearDetallesVentaAsync(ventaId, pedido);
                var factura = await CrearFacturaAsync(ventaId, pedido, montoPagado, "USD", MetodoPago.Efectivo, tasaCambio);

              
                var facturaDto = await ObtenerFacturaCompletaAsync(factura.Id_Factura);

                
                _ = Task.Run(() => GenerarYGuardarPdfAsync(factura.Id_Factura));

                _logger.LogInformation("✅ Factura en dólares {FacturaId} generada exitosamente", factura.Id_Factura);

                return ResultadoFacturacion.Exitoso(facturaDto, "Factura en dólares generada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error al generar factura en dólares para pedido {PedidoId}", pedidoId);
                return ResultadoFacturacion.Error($"Error al generar factura: {ex.Message}");
            }
        }

        public async Task<ResultadoFacturacion> GenerarFacturaMixtaAsync(int pedidoId, List<PagoFacturaDto> pagos)
        {
            try
            {
                _logger.LogInformation("💳 Generando factura mixta para pedido {PedidoId}", pedidoId);

                var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);
                var errores = await ValidarPedidoParaFacturacionAsync(pedido);

                if (errores.Any())
                {
                    return ResultadoFacturacion.Error("No se puede generar la factura", errores);
                }

                
                decimal totalPagado = 0;
                foreach (var pago in pagos)
                {
                    if (pago.Moneda == "USD")
                    {
                        if (!pago.TasaCambio.HasValue || pago.TasaCambio <= 0)
                            return ResultadoFacturacion.Error("Tasa de cambio requerida para pagos en USD");
                        
                        totalPagado += pago.Monto * pago.TasaCambio.Value;
                    }
                    else
                    {
                        totalPagado += pago.Monto;
                    }
                }

                
                if (totalPagado < pedido.Total - 0.01m)
                {
                    return ResultadoFacturacion.Error($"El monto total pagado ({totalPagado:C}) es menor al total del pedido ({pedido.Total:C})");
                }

               
                var ventaId = await CrearVentaSimpleAsync(pedido, MetodoPago.Mixto);
                await CrearDetallesVentaAsync(ventaId, pedido);

                
                var factura = new Factura
                {
                    Id_Venta = ventaId,
                    FechaEmision = DateTime.Now,
                    Total = pedido.Total,
                    Cliente = pedido.Cliente,
                    Estado = EstadoFactura.Activa,
                    MontoPagado = totalPagado,
                    Moneda = "CRC",
                    MetodoPago = "Mixto",
                    Cambio = Math.Max(0, totalPagado - pedido.Total)
                };

                _context.Factura.Add(factura);
                await _context.SaveChangesAsync();

                
                foreach (var pagoDto in pagos)
                {
                    var pago = new PagoFactura
                    {
                        FacturaId = factura.Id_Factura,
                        MetodoPago = pagoDto.MetodoPago,
                        Monto = pagoDto.Monto,
                        Moneda = pagoDto.Moneda,
                        TasaCambio = pagoDto.TasaCambio
                    };
                    _context.PagoFactura.Add(pago);
                }
                await _context.SaveChangesAsync();

                var numeroFactura = $"F-{factura.Id_Factura:0000}";
                await _pedidoService.ActualizarPedidoConFactura(ventaId, factura.Id_Factura, numeroFactura);

               
                var facturaDto = await ObtenerFacturaCompletaAsync(factura.Id_Factura);

                
                _ = Task.Run(() => GenerarYGuardarPdfAsync(factura.Id_Factura));

                _logger.LogInformation("✅ Factura mixta {FacturaId} generada exitosamente", factura.Id_Factura);

                return ResultadoFacturacion.Exitoso(facturaDto, "Factura generada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error al generar factura mixta para pedido {PedidoId}", pedidoId);
                return ResultadoFacturacion.Error($"Error al generar factura: {ex.Message}");
            }
        }

        public async Task<FacturaDto> ObtenerFacturaAsync(int facturaId)
        {
            return await ObtenerFacturaCompletaAsync(facturaId);
        }

        public async Task<List<FacturaDto>> BuscarFacturasAsync(DateTime? fechaInicio, DateTime? fechaFin, int? numeroFactura, string? cliente)
        {
            try
            {
                var query = _context.Factura.AsQueryable();

                if (numeroFactura.HasValue)
                {
                    query = query.Where(f => f.Id_Factura == numeroFactura.Value);
                }
                else
                {
                    if (fechaInicio.HasValue)
                    {
                        query = query.Where(f => f.FechaEmision.Date >= fechaInicio.Value.Date);
                    }

                    if (fechaFin.HasValue)
                    {
                        query = query.Where(f => f.FechaEmision.Date <= fechaFin.Value.Date);
                    }

                    if (!string.IsNullOrEmpty(cliente))
                    {
                        var clienteBusqueda = cliente.ToLower();
                        query = query.Where(f => f.Cliente != null && f.Cliente.ToLower().Contains(clienteBusqueda));
                    }
                }

                var facturas = await query
                    .OrderByDescending(f => f.FechaEmision)
                    .ToListAsync();

                var facturasDto = new List<FacturaDto>();

                foreach (var factura in facturas)
                {
                    var detalles = await _context.DetalleVenta
                        .Where(d => d.Id_Venta == factura.Id_Venta)
                        .ToListAsync();
                    
                    facturasDto.Add(MapearFacturaADto(factura, detalles));
                }

                return facturasDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar facturas");
                throw;
            }
        }

        public async Task<bool> AnularFacturaAsync(int facturaId, string justificacion)
        {
            try
            {
                var factura = await _context.Factura.FindAsync(facturaId);
                if (factura == null)
                {
                    throw new Exception("Factura no encontrada");
                }

                if (factura.Estado == EstadoFactura.Anulada)
                {
                    throw new Exception("La factura ya está anulada");
                }

                var fechaFactura = factura.FechaEmision.Date;
                var cajaCerrada = await _context.Caja
                    .AnyAsync(c => c.Fecha_Apertura.Date == fechaFactura && c.Fecha_Cierre != null);

                if (cajaCerrada)
                {
                    throw new Exception("No se puede anular una factura de un día cerrado.");
                }

                factura.Estado = EstadoFactura.Anulada;
                factura.Justificacion = justificacion;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Factura {FacturaId} anulada. Justificación: {Justificacion}", facturaId, justificacion);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al anular factura {FacturaId}", facturaId);
                throw;
            }
        }

        public async Task<int> GenerarNotaCreditoAsync(int facturaId, List<int> productosIds)
        {
            try
            {
                var facturaOriginal = await _context.Factura
                    .Include(f => f.Venta)
                    .FirstOrDefaultAsync(f => f.Id_Factura == facturaId);

                if (facturaOriginal == null) throw new Exception("Factura original no encontrada");
                if (facturaOriginal.Estado != EstadoFactura.Activa) throw new Exception("La factura no está activa");

                
                var detallesOriginales = await _context.DetalleVenta
                    .Where(d => d.Id_Venta == facturaOriginal.Id_Venta && productosIds.Contains(d.Id_Producto))
                    .ToListAsync();

                if (!detallesOriginales.Any()) throw new Exception("No se encontraron productos para devolver");

                decimal totalDevolucion = detallesOriginales.Sum(d => d.Cantidad * d.PrecioUnitario - (d.Descuento ?? 0));

                
                var notaCredito = new Factura
                {
                    Id_Venta = facturaOriginal.Id_Venta,
                    FechaEmision = DateTime.Now,
                    Total = -totalDevolucion, 
                    Cliente = facturaOriginal.Cliente,
                    Estado = EstadoFactura.NotaCredito,
                    MontoPagado = 0,
                    Moneda = facturaOriginal.Moneda,
                    MetodoPago = facturaOriginal.MetodoPago,
                    TasaCambio = facturaOriginal.TasaCambio,
                    FacturaOriginalId = facturaId,
                    Justificacion = "Devolución de productos"
                };

                _context.Factura.Add(notaCredito);
                await _context.SaveChangesAsync();

                
                _ = Task.Run(() => GenerarYGuardarPdfAsync(notaCredito.Id_Factura));

                _logger.LogInformation("Nota de Crédito {Id} generada para Factura {FacturaId}", notaCredito.Id_Factura, facturaId);
                return notaCredito.Id_Factura;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar nota de crédito para factura {FacturaId}", facturaId);
                throw;
            }
        }

        public (bool EsValido, string Mensaje) ValidarMontoPago(decimal totalPedido, decimal montoPagado, string moneda, decimal? tasaCambio)
        {
            decimal totalAPagar = totalPedido;

            if (moneda == "USD")
            {
                if (!tasaCambio.HasValue || tasaCambio <= 0)
                {
                    return (false, "La tasa de cambio es requerida para pagos en dólares");
                }
                totalAPagar = totalPedido / tasaCambio.Value;
            }

            if (montoPagado < totalAPagar)
            {
                return (false, $"El monto pagado ({montoPagado:C}) es menor al total del pedido ({totalAPagar:C})");
            }

            return (true, string.Empty);
        }

        public async Task<decimal> GetVentasDiaAsync()
        {
            var hoy = DateTime.Today;
            return await _context.Venta
                .Where(v => v.Fecha.Date == hoy)
                .SumAsync(v => v.Total);
        }

        public async Task<decimal> GetVentasSemanaAsync()
        {
            var hoy = DateTime.Today;
            var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek);
            return await _context.Venta
                .Where(v => v.Fecha.Date >= inicioSemana && v.Fecha.Date <= hoy)
                .SumAsync(v => v.Total);
        }

        public async Task<decimal> GetVentasMesAsync()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            return await _context.Venta
                .Where(v => v.Fecha.Date >= inicioMes && v.Fecha.Date <= hoy)
                .SumAsync(v => v.Total);
        }

        #region Métodos Privados

        private async Task<int> CrearVentaSimpleAsync(PedidoDto pedido, MetodoPago metodoPago)
        {
            var usuarioId = await ObtenerUsuarioIdAutenticado();

            if (usuarioId == 0)
            {
                throw new Exception("No se pudo determinar el usuario autenticado");
            }

            var venta = new Venta
            {
                Fecha = DateTime.Now,
                Total = pedido.Total,
                MetodoPago = metodoPago.ToString(),
                Id_Usuario = usuarioId,
                Estado = true
            };

            _context.Venta.Add(venta);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Venta {VentaId} creada para pedido", venta.Id_Venta);
            return venta.Id_Venta;
        }

        private async Task<List<string>> ValidarPedidoParaFacturacionAsync(PedidoDto pedido)
        {
            var errores = new List<string>();

            if (pedido == null)
            {
                errores.Add("El pedido no existe");
                return errores;
            }

            if (!pedido.Items.Any())
            {
                errores.Add("El pedido no tiene productos");
            }

           
            foreach (var item in pedido.Items)
            {
                if (item.PrecioUnitario <= 0)
                {
                    errores.Add($"El producto '{item.NombreProducto}' no tiene precio válido");
                }

                if (item.Cantidad <= 0)
                {
                    errores.Add($"El producto '{item.NombreProducto}' tiene cantidad inválida");
                }

                
                var productoExiste = await _context.Producto
                    .AnyAsync(p => p.Id_Producto == item.Id_Producto && p.Estado);

                if (!productoExiste)
                {
                    errores.Add($"El producto '{item.NombreProducto}' no existe en el catálogo");
                }
            }

            if (string.IsNullOrWhiteSpace(pedido.Cliente))
            {
                errores.Add("El pedido no tiene cliente asignado");
            }

            return errores;
        }

        private async Task CrearDetallesVentaAsync(int ventaId, PedidoDto pedido)
        {
            var detalles = pedido.Items.Select(item => new DetalleVenta
            {
                Id_Venta = ventaId,
                Id_Producto = item.Id_Producto,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                Descuento = item.Descuento
            }).ToList();

            _context.DetalleVenta.AddRange(detalles);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ {Count} detalles de venta creados para venta {VentaId}", detalles.Count, ventaId);
        }

        private async Task<Factura> CrearFacturaAsync(int ventaId, PedidoDto pedido, decimal montoPagado,
            string moneda, MetodoPago metodoPago, decimal? tasaCambio = null)
        {
            var factura = new Factura
            {
                Id_Venta = ventaId,
                FechaEmision = DateTime.Now,
                Total = pedido.Total,
                Cliente = pedido.Cliente,
                Estado = EstadoFactura.Activa,
                MontoPagado = montoPagado,
                Moneda = moneda,
                MetodoPago = metodoPago.ToString(),
                TasaCambio = tasaCambio
            };

            factura.CalcularCambio();
            _context.Factura.Add(factura);
            await _context.SaveChangesAsync();

            var numeroFactura = $"F-{factura.Id_Factura:0000}";
            await _pedidoService.ActualizarPedidoConFactura(ventaId, factura.Id_Factura, numeroFactura);

            _logger.LogInformation("✅ Factura {FacturaId} creada para venta {VentaId}", factura.Id_Factura, ventaId);
            return factura;
        }

        private async Task GenerarYGuardarPdfAsync(int facturaId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();

            try
            {
                _logger.LogInformation("📄 Generando PDF para factura {FacturaId}", facturaId);

                var facturaDto = await ObtenerFacturaCompletaAsync(facturaId, context);
                if (facturaDto == null)
                {
                    _logger.LogWarning("No se pudo obtener factura {FacturaId} para generar PDF", facturaId);
                    return;
                }

                byte[] pdfBytes = pdfService.GenerarFacturaPdf(facturaDto);

                var facturaEntity = await context.Factura.FindAsync(facturaId);
                if (facturaEntity != null)
                {
                    facturaEntity.PdfData = pdfBytes;
                    facturaEntity.PdfFileName = $"factura-{facturaId}.pdf";
                    await context.SaveChangesAsync();

                    _logger.LogInformation("✅ PDF guardado en BD para factura {FacturaId}", facturaId);
                }
            }
            catch (Exception pdfEx)
            {
                _logger.LogError(pdfEx, "⚠️ Error al generar PDF para factura {FacturaId}", facturaId);
            }
        }

        private async Task<FacturaDto> ObtenerFacturaCompletaAsync(int facturaId, ApplicationDbContext context = null)
        {
            var dbContext = context ?? _context; 

            try
            {
                var factura = await dbContext.Factura
                    .Include(f => f.Pagos)
                    .FirstOrDefaultAsync(f => f.Id_Factura == facturaId);

                if (factura == null)
                {
                    _logger.LogWarning("Factura {FacturaId} no encontrada", facturaId);
                    return null;
                }

                var detalles = await dbContext.DetalleVenta
                    .Include(d => d.Producto)
                    .Where(d => d.Id_Venta == factura.Id_Venta)
                    .ToListAsync();

                return MapearFacturaADto(factura, detalles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener factura {FacturaId}", facturaId);
                throw;
            }
        }

        private FacturaDto MapearFacturaADto(Factura factura, List<DetalleVenta> detalles)
        {
            var subtotal = detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
            var totalDescuentos = detalles.Sum(d => d.Descuento ?? 0);
            var impuestos = factura.Total - subtotal + totalDescuentos;

            return new FacturaDto
            {
                Id_Factura = factura.Id_Factura,
                PedidoId = factura.Id_Venta,
                NumeroFactura = $"F-{factura.Id_Factura:0000}",
                FechaEmision = factura.FechaEmision,
                Cliente = factura.Cliente ?? "Cliente Genérico",
                Subtotal = subtotal,
                Impuestos = impuestos > 0 ? impuestos : 0,
                Total = factura.Total,
                MontoPagado = factura.MontoPagado,
                Cambio = factura.Cambio,
                Moneda = factura.Moneda,
                MetodoPago = Enum.TryParse<MetodoPago>(factura.MetodoPago, out var metodo) ? metodo : MetodoPago.Mixto,
                TasaCambio = factura.TasaCambio,
                EstadoFactura = factura.Estado,
                Items = detalles.Select(d => new ItemFacturaDto
                {
                    ProductoId = d.Id_Producto,
                    NombreProducto = d.Producto?.Nombre ?? "Producto no disponible",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Cantidad * d.PrecioUnitario - (d.Descuento ?? 0),
                    Descuento = d.Descuento
                }).ToList(),
                Pagos = factura.Pagos.Select(p => new PagoFacturaDto
                {
                    Id = p.Id,
                    FacturaId = p.FacturaId,
                    MetodoPago = p.MetodoPago,
                    Monto = p.Monto,
                    Moneda = p.Moneda,
                    TasaCambio = p.TasaCambio
                }).ToList()
            };
        }

        private async Task<int> ObtenerUsuarioIdAutenticado()
        {
            try
            {
                
                var usuarioIdSession = _httpContextAccessor.HttpContext?.Session.GetInt32("UsuarioId");
                if (usuarioIdSession.HasValue && usuarioIdSession.Value > 0)
                {
                    return usuarioIdSession.Value;
                }

               
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("UsuarioId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }

               
                var usuarioDefault = await _context.Usuario
                    .Where(u => u.Estado)
                    .OrderBy(u => u.Id_Usr)
                    .FirstOrDefaultAsync();

                if (usuarioDefault != null)
                {
                    _logger.LogWarning("Usando usuario por defecto: {UsuarioId}", usuarioDefault.Id_Usr);
                    return usuarioDefault.Id_Usr;
                }

                throw new Exception("No se pudo determinar el usuario autenticado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario autenticado");
                throw;
            }
        }

        #endregion
    }
}