using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Enums;
using VentaFacil.web.Services.Producto;
using Microsoft.Extensions.Logging;

namespace VentaFacil.web.Services.Pedido
{
    public class PedidoService : IPedidoService
    {
        private static readonly ConcurrentDictionary<int, PedidoDto> _pedidosTemporales = new();
        private static int _contadorId = 1;
        private static readonly object _lock = new object();
        private readonly IProductoService _productoService;
        private readonly ILogger<PedidoService> _logger;

        public PedidoService(IProductoService productoService, ILogger<PedidoService> logger)
        {
            _productoService = productoService;
            _logger = logger;
        }

        public Task<PedidoDto> CrearPedidoAsync(int idUsuario, string? cliente = null)
        {
            lock (_lock)
            {
                var nuevoPedido = new PedidoDto
                {
                    Id_Venta = _contadorId++,
                    Fecha = DateTime.Now,
                    Estado = PedidoEstado.Borrador,
                    Id_Usuario = idUsuario,
                    Cliente = cliente,
                    Modalidad = ModalidadPedido.ParaLlevar,
                    Items = new List<PedidoItemDto>()
                };

                _pedidosTemporales[nuevoPedido.Id_Venta] = nuevoPedido;

                return Task.FromResult(nuevoPedido);
            }
        }

        public Task<PedidoDto> ObtenerPedidoAsync(int idPedido)
        {
            if (_pedidosTemporales.TryGetValue(idPedido, out var pedido))
            {
                return Task.FromResult(pedido);
            }

            throw new KeyNotFoundException($"Pedido con ID {idPedido} no encontrado");
        }

        public async Task<PedidoDto> AgregarProductoAsync(int idPedido, int idProducto, int cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a 0");

            var pedido = await ObtenerPedidoAsync(idPedido);

            if (!await PuedeEditarseAsync(idPedido))
                throw new InvalidOperationException("No se puede modificar un pedido que no está en estado borrador");

            var producto = await _productoService.ObtenerPorIdAsync(idProducto);
            if (producto == null)
                throw new ArgumentException($"Producto con ID {idProducto} no encontrado");

            var itemExistente = pedido.Items.FirstOrDefault(i => i.Id_Producto == idProducto);

            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                var nuevoItem = new PedidoItemDto
                {
                    Id_Detalle = pedido.Items.Count > 0 ? pedido.Items.Max(i => i.Id_Detalle) + 1 : 1,
                    Id_Producto = producto.Id_Producto,
                    NombreProducto = producto.Nombre ?? "Producto sin nombre",
                    PrecioUnitario = producto.Precio,
                    Cantidad = cantidad
                };
                pedido.Items.Add(nuevoItem);
            }

            RecalcularTotal(pedido);
            return pedido;
        }

        public async Task<PedidoDto> ActualizarCantidadProductoAsync(int idPedido, int idDetalle, int cantidad)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            if (!await PuedeEditarseAsync(idPedido))
                throw new InvalidOperationException("No se puede modificar un pedido que no está en estado borrador");

            var item = pedido.Items.FirstOrDefault(i => i.Id_Detalle == idDetalle);
            if (item == null)
                throw new ArgumentException($"Item con ID {idDetalle} no encontrado en el pedido");

            if (cantidad <= 0)
            {
                pedido.Items.Remove(item);
            }
            else
            {
                item.Cantidad = cantidad;
            }

            RecalcularTotal(pedido);
            return pedido;
        }

        public async Task<PedidoDto> EliminarProductoAsync(int idPedido, int idDetalle)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            if (!await PuedeEditarseAsync(idPedido))
                throw new InvalidOperationException("No se puede modificar un pedido que no está en estado borrador");

            var item = pedido.Items.FirstOrDefault(i => i.Id_Detalle == idDetalle);
            if (item != null)
            {
                pedido.Items.Remove(item);
                RecalcularTotal(pedido);
            }

            return pedido;
        }

        public async Task<PedidoDto> ActualizarModalidadAsync(int pedidoId, ModalidadPedido modalidad, int? numeroMesa)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            if (!await PuedeEditarseAsync(pedidoId))
                throw new InvalidOperationException("El pedido no puede ser modificado");

            pedido.Modalidad = modalidad;

            if (modalidad == ModalidadPedido.ParaLlevar)
            {
                pedido.NumeroMesa = null;
            }
            else
            {
                pedido.NumeroMesa = numeroMesa;
            }

            return pedido;
        }

        public async Task<PedidoDto> ActualizarClienteAsync(int pedidoId, string cliente)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            if (!await PuedeEditarseAsync(pedidoId))
                throw new InvalidOperationException("El pedido no puede ser modificado");

            pedido.Cliente = cliente?.Trim();

            return pedido;
        }

        public async Task<bool> ValidarPedidoParaGuardarAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            if (!pedido.Items.Any())
                return false;

            if (pedido.Modalidad == ModalidadPedido.EnMesa && (!pedido.NumeroMesa.HasValue || pedido.NumeroMesa <= 0))
                return false;

            if (string.IsNullOrWhiteSpace(pedido.Cliente))
                return false;

            return true;
        }

        public async Task<bool> ValidarModalidadMesaAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);
            return pedido.Modalidad == ModalidadPedido.EnMesa && pedido.NumeroMesa.HasValue && pedido.NumeroMesa > 0;
        }

        public async Task<ServiceResult> GuardarPedidoAsync(int idPedido)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            _logger.LogInformation("=== GUARDAR PEDIDO SERVICE ===");
            _logger.LogInformation("Pedido ID: {PedidoId}", idPedido);
            _logger.LogInformation("Estado antes: {Estado}", pedido.Estado);

            var esValido = await ValidarPedidoParaGuardarAsync(idPedido);
            if (!esValido)
            {
                _logger.LogWarning("VALIDACIÓN FALLIDA");
                return ServiceResult.Error("No se puede guardar el pedido. Verifique que tenga productos, cliente y si es en mesa, número de mesa válido");
            }

            
            pedido.Estado = PedidoEstado.Pendiente;
            pedido.Fecha = DateTime.Now;

            _logger.LogInformation("Estado después: {Estado}", pedido.Estado);
            _logger.LogInformation("=== GUARDADO EXITOSO ===");

            return ServiceResult.SuccessResult($"Pedido #{pedido.Id_Venta} guardado correctamente. Proceda al pago.", pedido);
        }

        public async Task<ServiceResult> GuardarComoBorradorAsync(int idPedido)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            pedido.Estado = PedidoEstado.Borrador;
            pedido.Fecha = DateTime.Now;

            return ServiceResult.SuccessResult($"Pedido #{pedido.Id_Venta} guardado como borrador correctamente", pedido);
        }

        public async Task<bool> PuedeEditarseAsync(int idPedido)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            return pedido.Estado == PedidoEstado.Borrador;
        }

        public Task<List<PedidoDto>> ObtenerPedidosBorradorAsync(int idUsuario)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario && p.Estado == PedidoEstado.Borrador)
                .ToList();

            return Task.FromResult(pedidos);
        }

        private void RecalcularTotal(PedidoDto pedido)
        {
            pedido.Total = pedido.Items.Sum(item => item.Subtotal);
        }

        public Task<List<PedidoDto>> ObtenerPedidosPendientesAsync(int idUsuario)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario &&
                           (p.Estado == PedidoEstado.Pendiente || p.Estado == PedidoEstado.EnPreparacion))
                .OrderByDescending(p => p.Fecha)
                .ToList();

            _logger.LogInformation("=== OBTENER PEDIDOS PENDIENTES ===");
            _logger.LogInformation("Usuario: {UsuarioId}", idUsuario);
            _logger.LogInformation("Total pedidos encontrados: {Count}", pedidos.Count);

            foreach (var pedido in pedidos)
            {
                _logger.LogInformation("Pedido #{PedidoId} - Estado: {Estado}", pedido.Id_Venta, pedido.Estado);
            }

            return Task.FromResult(pedidos);
        }

        public Task<List<PedidoDto>> ObtenerTodosLosPedidosAsync(int idUsuario)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario)
                .OrderByDescending(p => p.Fecha)
                .ToList();

            return Task.FromResult(pedidos);
        }

        public async Task<List<PedidoDto>> ObtenerPedidosParaCocinaAsync()
        {
            return await Task.FromResult(_pedidosTemporales.Values
                .Where(p => p.Estado == PedidoEstado.Pendiente || p.Estado == PedidoEstado.EnPreparacion || p.Estado == PedidoEstado.Listo)
                .OrderBy(p => p.Fecha)
                .ToList());
        }

        public async Task<ServiceResult> MarcarComoListoAsync(int pedidoId)
        {
            _logger.LogInformation("=== SERVICIO: MarcarComoListoAsync ===");
            _logger.LogInformation("Buscando pedido ID: {PedidoId}", pedidoId);

            var pedido = await ObtenerPedidoAsync(pedidoId);

            _logger.LogInformation("Pedido encontrado - ID: {PedidoId}, Estado actual: {Estado}", pedido.Id_Venta, pedido.Estado);

            if (pedido.Estado != PedidoEstado.EnPreparacion)
            {
                _logger.LogWarning("ERROR: Estado inválido. Esperado: EnPreparacion, Actual: {Estado}", pedido.Estado);
                return ServiceResult.Error("Solo se pueden marcar como listo pedidos en preparación");
            }

            pedido.Estado = PedidoEstado.Listo;
            pedido.FechaActualizacion = DateTime.Now;

            _logger.LogInformation("Pedido actualizado - Nuevo estado: {Estado}", pedido.Estado);
            _logger.LogInformation("Total de pedidos en memoria: {Count}", _pedidosTemporales.Count);

            if (_pedidosTemporales.TryGetValue(pedidoId, out var pedidoVerificado))
            {
                _logger.LogInformation("Pedido verificado en diccionario - Estado: {Estado}", pedidoVerificado.Estado);
            }
            else
            {
                _logger.LogError("ERROR: Pedido no encontrado en diccionario después de actualizar");
            }

            return ServiceResult.SuccessResult("Pedido marcado como listo para entregar");
        }

        public async Task<ServiceResult> AgregarNotaCocinaAsync(int pedidoId, string nota)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);
            pedido.Notas = nota;
            return ServiceResult.SuccessResult("Nota agregada correctamente");
        }

        public async Task<ServiceResult> CancelarPedidoAsync(int pedidoId, string razon)
        {
            _logger.LogInformation("=== SERVICIO: CancelarPedidoAsync ===");
            _logger.LogInformation("Pedido ID: {PedidoId}, Razón: {Razon}", pedidoId, razon);

            var pedido = await ObtenerPedidoAsync(pedidoId);

            _logger.LogInformation("Pedido encontrado - ID: {PedidoId}, Estado actual: {Estado}", pedido.Id_Venta, pedido.Estado);

            if (pedido.Estado == PedidoEstado.Entregado)
            {
                _logger.LogWarning("ERROR: No se puede cancelar pedido entregado");
                return ServiceResult.Error("No se puede cancelar un pedido ya entregado");
            }

            if (pedido.Estado == PedidoEstado.Cancelado)
            {
                _logger.LogWarning("ERROR: Pedido ya cancelado");
                return ServiceResult.Error("El pedido ya está cancelado");
            }

            pedido.Estado = PedidoEstado.Cancelado;

            
            pedido.MotivoCancelacion = $"CANCELADO: {razon}";
            pedido.FechaActualizacion = DateTime.Now;

            _logger.LogInformation("Pedido cancelado - Nuevo estado: {Estado}, Motivo: {Motivo}", pedido.Estado, pedido.MotivoCancelacion);

            return ServiceResult.SuccessResult("Pedido cancelado correctamente");
        }

        public async Task<List<PedidoDto>> BuscarPedidosAsync(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return new List<PedidoDto>();

            return _pedidosTemporales.Values
                .Where(p =>
                    (p.Cliente?.Contains(termino, StringComparison.OrdinalIgnoreCase) == true) ||
                    (p.NumeroMesa?.ToString()?.Contains(termino) == true) ||
                    p.Id_Venta.ToString().Contains(termino) ||
                    p.Items.Any(i => i.NombreProducto.Contains(termino, StringComparison.OrdinalIgnoreCase))
                )
                .Take(10)
                .ToList();
        }

        public async Task<ServiceResult> MarcarComoEntregadoAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            if (pedido.Estado != PedidoEstado.Listo)
                return ServiceResult.Error("Solo se pueden marcar como entregado pedidos listos");

            pedido.Estado = PedidoEstado.Entregado;
            return ServiceResult.SuccessResult("Pedido marcado como entregado correctamente");
        }

        public async Task<ServiceResult> IniciarPreparacionAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            _logger.LogInformation("=== INICIAR PREPARACION SERVICE ===");
            _logger.LogInformation("Pedido ID: {PedidoId}", pedidoId);
            _logger.LogInformation("Estado actual: {Estado}", pedido.Estado);

            if (pedido.Estado != PedidoEstado.Pendiente && pedido.Estado != PedidoEstado.Borrador)
            {
                _logger.LogWarning("ERROR: Estado inválido. Esperado: Pendiente o Borrador, Actual: {Estado}", pedido.Estado);
                return ServiceResult.Error("Solo se pueden iniciar preparación de pedidos pendientes o en borrador");
            }

            pedido.Estado = PedidoEstado.EnPreparacion;
            pedido.FechaActualizacion = DateTime.Now;

            _logger.LogInformation("Nuevo estado: {Estado}", pedido.Estado);
            _logger.LogInformation("Total pedidos en memoria: {Count}", _pedidosTemporales.Count);

            if (_pedidosTemporales.TryGetValue(pedidoId, out var pedidoVerificado))
            {
                _logger.LogInformation("Pedido verificado - Estado: {Estado}", pedidoVerificado.Estado);
            }

            return ServiceResult.SuccessResult("Preparación del pedido iniciada");
        }

        private bool EsTransicionValida(PedidoEstado estadoActual, PedidoEstado nuevoEstado)
        {
            var transicionesValidas = new Dictionary<PedidoEstado, List<PedidoEstado>>
            {
                { PedidoEstado.Borrador, new List<PedidoEstado> { PedidoEstado.EnPreparacion, PedidoEstado.Cancelado } },
                { PedidoEstado.EnPreparacion, new List<PedidoEstado> { PedidoEstado.Listo, PedidoEstado.Cancelado } },
                { PedidoEstado.Listo, new List<PedidoEstado> { PedidoEstado.Entregado, PedidoEstado.Cancelado } },
                { PedidoEstado.Entregado, new List<PedidoEstado>() },
                { PedidoEstado.Cancelado, new List<PedidoEstado>() }
            };

            return transicionesValidas[estadoActual].Contains(nuevoEstado);
        }

        public async Task<ResumenPedidosDto> ObtenerResumenPedidosAsync(int usuarioId)
        {
            var borrador = await ObtenerPedidosBorradorAsync(usuarioId);
            var pendientes = await ObtenerPedidosPendientesAsync(usuarioId);
            var todos = await ObtenerTodosLosPedidosAsync(usuarioId);

            return new ResumenPedidosDto
            {
                TotalBorrador = borrador.Count,
                TotalEnCocina = pendientes.Count,
                TotalListos = todos.Count(p => p.Estado == PedidoEstado.Listo),
                TotalEntregados = todos.Count(p => p.Estado == PedidoEstado.Entregado),
                TotalCancelados = todos.Count(p => p.Estado == PedidoEstado.Cancelado)
            };
        }

        public Task<List<PedidoDto>> ObtenerPedidosListosAsync(int idUsuario)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario && p.Estado == PedidoEstado.Listo)
                .OrderByDescending(p => p.Fecha)
                .ToList();

            return Task.FromResult(pedidos);
        }

        public Task<List<PedidoDto>> ObtenerPedidosEntregadosAsync(int idUsuario)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario && p.Estado == PedidoEstado.Entregado)
                .OrderByDescending(p => p.Fecha)
                .ToList();

            return Task.FromResult(pedidos);
        }

        public Task<List<PedidoDto>> ObtenerPedidosCanceladosAsync(int idUsuario)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario && p.Estado == PedidoEstado.Cancelado)
                .OrderByDescending(p => p.Fecha)
                .ToList();

            return Task.FromResult(pedidos);
        }

        public void VerificarEstadoPedidos(int usuarioId)
        {
            _logger.LogInformation("=== VERIFICACIÓN ESTADO PEDIDOS ===");
            _logger.LogInformation("Total pedidos en memoria: {Count}", _pedidosTemporales.Count);

            var pedidosUsuario = _pedidosTemporales.Values.Where(p => p.Id_Usuario == usuarioId).ToList();
            _logger.LogInformation("Pedidos del usuario {UsuarioId}: {Count}", usuarioId, pedidosUsuario.Count);

            foreach (var pedido in pedidosUsuario)
            {
                _logger.LogInformation("Pedido #{PedidoId} - Estado: {Estado} - Cliente: {Cliente} - Items: {ItemsCount}",
                    pedido.Id_Venta, pedido.Estado, pedido.Cliente, pedido.Items.Count);
            }
        }

        public Task<List<PedidoDto>> ObtenerPedidosConFacturaAsync(int idUsuario)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario && p.FacturaId.HasValue && p.FacturaId > 0)
                .OrderByDescending(p => p.Fecha)
                .ToList();

            return Task.FromResult(pedidos);
        }

        public async Task<bool> TieneFacturaAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);
            return pedido.FacturaId.HasValue && pedido.FacturaId > 0;
        }

        public async Task<int?> ObtenerFacturaIdAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);
            return pedido.FacturaId;
        }

        public async Task ActualizarPedidoConFactura(int pedidoId, int facturaId, string numeroFactura)
        {
            try
            {
                if (_pedidosTemporales.TryGetValue(pedidoId, out var pedidoEnMemoria))
                {
                    pedidoEnMemoria.FacturaId = facturaId;
                    pedidoEnMemoria.NumeroFactura = numeroFactura; // GUARDAR EL NÚMERO DE FACTURA
                    _logger.LogInformation("✅ Pedido {PedidoId} actualizado con Factura {FacturaId} - {NumeroFactura}",
                        pedidoId, facturaId, numeroFactura);
                }
                else
                {
                    _logger.LogWarning("⚠️ No se encontró el pedido {PedidoId} en memoria para actualizar con factura", pedidoId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al actualizar pedido {PedidoId} con factura {FacturaId}", pedidoId, facturaId);
            }
        }
    }
}