using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Enums;
using VentaFacil.web.Models.Response.Factura;
using VentaFacil.web.Services.Pedido;

namespace VentaFacil.web.Services.Facturacion
{
    public class FacturacionService : IFacturacionService
    {
        private readonly IPedidoService _pedidoService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FacturacionService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FacturacionService(
            IPedidoService pedidoService,
            ApplicationDbContext context,
            ILogger<FacturacionService> logger,
            IHttpContextAccessor httpContextAccessor) 
        {
            _pedidoService = pedidoService;
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ResultadoFacturacion> GenerarFacturaAsync(int pedidoId, MetodoPago metodoPago, decimal montoPagado, string moneda = "CRC")
        {
            try
            {
                _logger.LogInformation("🔧 Generando factura para pedido {PedidoId}", pedidoId);

                var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);
                var errores = ValidarPedidoParaFacturacion(pedido);

                if (errores.Any())
                {
                    _logger.LogWarning("❌ Validación fallida para pedido {PedidoId}: {Errores}", pedidoId, string.Join(", ", errores));
                    return ResultadoFacturacion.Error("No se puede generar la factura", errores);
                }

                
                var venta = await CrearVentaDesdePedido(pedido, metodoPago, moneda);
                _logger.LogInformation("✅ Venta creada - ID: {VentaId}", venta.Id_Venta);

                
                await CrearDetallesVenta(venta.Id_Venta, pedido);
                _logger.LogInformation("✅ Detalles de venta creados");

                
                var factura = await CrearFactura(
                    venta.Id_Venta,
                    pedido.Total,
                    pedido.Cliente,
                    montoPagado,
                    moneda,
                    metodoPago
                );

                
                if (factura?.Id_Factura > 0)
                {
                    var facturaDto = await ObtenerFacturaAsync(factura.Id_Factura);

                    _logger.LogInformation("🎯 Retornando ResultadoFacturacion - FacturaId: {FacturaId}, DTO Id: {DtoId}, Success: true",
                        factura.Id_Factura, facturaDto?.Id);

                    return ResultadoFacturacion.Exitoso(facturaDto, "Factura generada exitosamente");
                }
                else
                {
                    _logger.LogError("💥 No se pudo crear la factura en la base de datos");
                    return ResultadoFacturacion.Error("Error al crear la factura en la base de datos");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error completo al generar factura para pedido {PedidoId}", pedidoId);
                return ResultadoFacturacion.Error($"Error al generar factura: {ex.Message}");
            }
        }

        public async Task<ResultadoFacturacion> GenerarFacturaDolaresAsync(int pedidoId, decimal montoPagado, decimal tasaCambio)
        {
            try
            {
                _logger.LogInformation("Generando factura en dólares para pedido {PedidoId}", pedidoId);

                var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);
                var errores = ValidarPedidoParaFacturacion(pedido);

                if (errores.Any())
                {
                    return ResultadoFacturacion.Error("No se puede generar la factura", errores);
                }

                
                var totalDolares = pedido.Total / tasaCambio;

                
                var venta = await CrearVentaDesdePedido(pedido, MetodoPago.Efectivo, "USD");
                await CrearDetallesVenta(venta.Id_Venta, pedido);

                
                var factura = await CrearFactura(
                    venta.Id_Venta,
                    pedido.Total,
                    pedido.Cliente,
                    montoPagado, 
                    "USD",
                    MetodoPago.Efectivo,
                    tasaCambio  
                );

                
                var facturaDto = await ObtenerFacturaAsync(factura.Id_Factura);

                _logger.LogInformation("Factura en dólares {FacturaId} generada exitosamente", factura.Id_Factura);

                return ResultadoFacturacion.Exitoso(facturaDto, "Factura en dólares generada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar factura en dólares para pedido {PedidoId}", pedidoId);
                return ResultadoFacturacion.Error($"Error al generar factura: {ex.Message}");
            }
        }

        public async Task<FacturaDto> ObtenerFacturaAsync(int facturaId)
        {
            try
            {
                var factura = await _context.Factura
                    .Include(f => f.Venta)
                        .ThenInclude(v => v.Detalles)
                            .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(f => f.Id_Factura == facturaId);

                if (factura == null)
                    return null;

                var venta = factura.Venta;

                
                var subtotal = venta.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
                var totalDescuentos = venta.Detalles.Sum(d => d.Descuento ?? 0);
                var impuestos = venta.Total - subtotal + totalDescuentos;

                var facturaDto = new FacturaDto
                {
                    Id = factura.Id_Factura,
                    PedidoId = factura.Id_Venta,
                    NumeroFactura = $"F-{factura.Id_Factura:0000}",
                    FechaEmision = factura.FechaEmision,
                    Cliente = factura.Cliente ?? "Cliente Generico",
                    Subtotal = subtotal,
                    Impuestos = impuestos > 0 ? impuestos : 0,
                    Total = factura.Total,
                    MontoPagado = factura.MontoPagado,
                    Cambio = factura.Cambio,
                    Moneda = factura.Moneda,
                    MetodoPago = Enum.Parse<MetodoPago>(factura.MetodoPago),
                    TasaCambio = factura.TasaCambio,
                    EstadoFactura = factura.Estado ? EstadoFactura.Activa : EstadoFactura.Anulada,
                    Items = venta.Detalles.Select(d => new ItemFacturaDto
                    {
                        ProductoId = d.Id_Producto,
                        NombreProducto = d.Producto?.Nombre ?? "Producto no disponible",
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal = d.Cantidad * d.PrecioUnitario - (d.Descuento ?? 0),
                        Descuento = d.Descuento
                    }).ToList()
                };

                return facturaDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener factura {FacturaId}", facturaId);
                throw;
            }
        }

        #region Métodos Privados

        private List<string> ValidarPedidoParaFacturacion(PedidoDto pedido)
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

            // Validar productos individuales
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

                // Verificar que el producto existe en la base de datos
                var productoExiste = _context.Producto.Any(p => p.Id_Producto == item.Id_Producto && p.Estado); // Cambiado de Productos a Producto
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

        private async Task<Venta> CrearVentaDesdePedido(PedidoDto pedido, MetodoPago metodoPago, string moneda)
        {
            // Obtener el usuario autenticado
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

            return venta;
        }

        private async Task CrearDetallesVenta(int ventaId, PedidoDto pedido)
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
        }

        private async Task<Factura> CrearFactura(int ventaId, decimal total, string cliente,
            decimal montoPagado, string moneda, MetodoPago metodoPago, decimal? tasaCambio = null)
        {
            var factura = new Factura
            {
                Id_Venta = ventaId,
                FechaEmision = DateTime.Now,
                Total = total,
                Cliente = cliente,
                Estado = true,
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

            return factura;
        }


        private async Task<int> ObtenerUsuarioIdAutenticado()
        {
            try
            {
                
                var usuarioIdSession = _httpContextAccessor.HttpContext?.Session.GetInt32("UsuarioId");
                if (usuarioIdSession.HasValue && usuarioIdSession.Value > 0)
                {
                    _logger.LogInformation("Usuario obtenido desde Session: {UsuarioId}", usuarioIdSession.Value);
                    return usuarioIdSession.Value;
                }

                
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("UsuarioId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogInformation("Usuario obtenido desde Claims: {UsuarioId}", userId);
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
