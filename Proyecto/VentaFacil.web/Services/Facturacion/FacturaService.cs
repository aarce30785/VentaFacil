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
                _logger.LogInformation("Generando factura para pedido {PedidoId}", pedidoId);

                // 1. Obtener y validar el pedido
                var pedido = await _pedidoService.ObtenerPedidoAsync(pedidoId);
                var errores = ValidarPedidoParaFacturacion(pedido);

                if (errores.Any())
                {
                    return ResultadoFacturacion.Error("No se puede generar la factura", errores);
                }

                // 2. Crear la venta en la base de datos
                var venta = await CrearVentaDesdePedido(pedido, metodoPago, moneda);

                // 3. Crear los detalles de venta
                await CrearDetallesVenta(venta.Id_Venta, pedido);

                // 4. Crear la factura
                var factura = await CrearFactura(venta.Id_Venta, pedido.Total);

                // 5. Crear el DTO de respuesta
                var facturaDto = await CrearFacturaDto(factura.Id_Factura, pedido, montoPagado, moneda, metodoPago);

                _logger.LogInformation("Factura {FacturaId} generada exitosamente", factura.Id_Factura);

                return ResultadoFacturacion.Exitoso(facturaDto, "Factura generada exitosamente");
            }
            catch (Exception ex)
            {
                // LOG DETALLADO DEL ERROR
                _logger.LogError(ex, "Error completo al generar factura para pedido {PedidoId}", pedidoId);

                // Obtener el error interno
                var innerException = ex.InnerException != null ? ex.InnerException.Message : "No hay inner exception";
                _logger.LogError("Inner Exception: {InnerException}", innerException);

                return ResultadoFacturacion.Error($"Error al generar factura: {ex.Message}. Inner: {innerException}");
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

                // Calcular total en dólares
                var totalDolares = pedido.Total / tasaCambio;
                var cambio = montoPagado - totalDolares;

                // Crear venta y factura
                var venta = await CrearVentaDesdePedido(pedido, MetodoPago.Efectivo, "USD");
                await CrearDetallesVenta(venta.Id_Venta, pedido);
                var factura = await CrearFactura(venta.Id_Venta, pedido.Total);

                // Crear DTO con información de conversión
                var facturaDto = await CrearFacturaDto(factura.Id_Factura, pedido, montoPagado, "USD", MetodoPago.Efectivo);
                facturaDto.TasaCambio = tasaCambio;
                facturaDto.TotalColones = pedido.Total;
                facturaDto.TotalDolares = totalDolares;
                facturaDto.Cambio = cambio;

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
                    .Include(f => f.Venta)
                        .ThenInclude(v => v.Usuario)
                    .FirstOrDefaultAsync(f => f.Id_Factura == facturaId);

                if (factura == null) return null;

                return new FacturaDto
                {
                    Id = factura.Id_Factura,
                    PedidoId = factura.Id_Venta,
                    NumeroFactura = $"F-{factura.Id_Factura:000000}",
                    FechaEmision = factura.FechaEmision,
                    Total = factura.Total,
                    Items = factura.Venta.Detalles.Select(d => new ItemFacturaDto
                    {
                        ProductoId = d.Id_Producto,
                        NombreProducto = d.Producto?.Nombre ?? "Producto no encontrado",
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal = d.PrecioUnitario * d.Cantidad - d.Descuento
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener factura {FacturaId}", facturaId);
                return null;
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
                Id_Usuario = usuarioId, // Usar el ID del usuario autenticado
                Estado = true
            };

            _context.Venta.Add(venta);
            await _context.SaveChangesAsync();

            return venta;
        }

        private async Task CrearDetallesVenta(int ventaId, PedidoDto pedido)
        {
            foreach (var item in pedido.Items)
            {
                var detalle = new DetalleVenta
                {
                    Id_Venta = ventaId,
                    Id_Producto = item.Id_Producto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Descuento = 0
                };

                _context.DetalleVenta.Add(detalle);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<Factura> CrearFactura(int ventaId, decimal total)
        {
            var factura = new Factura
            {
                Id_Venta = ventaId,
                FechaEmision = DateTime.Now,
                Total = total,
                Estado = true
            };

            _context.Factura.Add(factura); // CORREGIDO: Cambiado de Facturas a Factura
            await _context.SaveChangesAsync();

            return factura;
        }

        private async Task<FacturaDto> CrearFacturaDto(int facturaId, PedidoDto pedido, decimal montoPagado, string moneda, MetodoPago metodoPago)
        {
            var factura = await ObtenerFacturaAsync(facturaId);

            if (factura != null)
            {
                factura.Cliente = pedido.Cliente;
                factura.MetodoPago = metodoPago;
                factura.MontoPagado = montoPagado;
                factura.Moneda = moneda;
                factura.Cambio = montoPagado - factura.Total;
                factura.EstadoFactura = EstadoFactura.Pagada;

                // Calcular impuestos (13% de IVA como ejemplo)
                factura.Impuestos = factura.Total * 0.13m;
                factura.Subtotal = factura.Total - factura.Impuestos;
            }

            return factura;
        }

        private async Task<int> ObtenerUsuarioIdAutenticado()
        {
            try
            {
                // PRIMERO intentar desde Session (más confiable)
                var usuarioIdSession = _httpContextAccessor.HttpContext?.Session.GetInt32("UsuarioId");
                if (usuarioIdSession.HasValue && usuarioIdSession.Value > 0)
                {
                    _logger.LogInformation("Usuario obtenido desde Session: {UsuarioId}", usuarioIdSession.Value);
                    return usuarioIdSession.Value;
                }

                // SEGUNDO intentar desde Claims
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("UsuarioId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogInformation("Usuario obtenido desde Claims: {UsuarioId}", userId);
                    return userId;
                }

                // TERCERO usar un usuario por defecto (TEMPORAL)
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
