using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Services.Producto;

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
                    Items = new List<PedidoItemDto>(),
                    HistorialEstados = new List<EstadoPedidoDto>
                    {
                        new EstadoPedidoDto
                        {
                            Estado = PedidoEstado.Borrador,
                            Fecha = DateTime.Now,
                            Observacion = "Pedido creado"
                        }
                    }
                };

                _pedidosTemporales[nuevoPedido.Id_Venta] = nuevoPedido;
                _logger.LogInformation("🆕 Pedido #{PedidoId} creado para usuario {UsuarioId}", nuevoPedido.Id_Venta, idUsuario);

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
            ValidarEstadoEditable(pedido.Estado);

            var producto = await _productoService.ObtenerPorIdAsync(idProducto);
            if (producto == null)
                throw new ArgumentException($"Producto con ID {idProducto} no encontrado");

            var itemExistente = pedido.Items.FirstOrDefault(i => i.Id_Producto == idProducto);

            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
                _logger.LogInformation("➕ Producto {ProductoId} actualizado a cantidad {Cantidad} en pedido #{PedidoId}",
                    idProducto, itemExistente.Cantidad, idPedido);
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
                _logger.LogInformation("🆕 Producto {ProductoId} agregado al pedido #{PedidoId}", idProducto, idPedido);
            }

            RecalcularTotal(pedido);
            return pedido;
        }

        public async Task<PedidoDto> ActualizarCantidadProductoAsync(int idPedido, int idDetalle, int cantidad)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);
            ValidarEstadoEditable(pedido.Estado);

            var item = pedido.Items.FirstOrDefault(i => i.Id_Detalle == idDetalle);
            if (item == null)
                throw new ArgumentException($"Item con ID {idDetalle} no encontrado en el pedido");

            if (cantidad <= 0)
            {
                pedido.Items.Remove(item);
                _logger.LogInformation("➖ Producto {ProductoId} eliminado del pedido #{PedidoId}", item.Id_Producto, idPedido);
            }
            else
            {
                item.Cantidad = cantidad;
                _logger.LogInformation("✏️ Cantidad del producto {ProductoId} actualizada a {Cantidad} en pedido #{PedidoId}",
                    item.Id_Producto, cantidad, idPedido);
            }

            RecalcularTotal(pedido);
            return pedido;
        }

        public async Task<PedidoDto> EliminarProductoAsync(int idPedido, int idDetalle)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);
            ValidarEstadoEditable(pedido.Estado);

            var item = pedido.Items.FirstOrDefault(i => i.Id_Detalle == idDetalle);
            if (item != null)
            {
                pedido.Items.Remove(item);
                RecalcularTotal(pedido);
                _logger.LogInformation("🗑️ Producto {ProductoId} eliminado del pedido #{PedidoId}", item.Id_Producto, idPedido);
            }

            return pedido;
        }

        public async Task<PedidoDto> ActualizarModalidadAsync(int pedidoId, ModalidadPedido modalidad, int? numeroMesa)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);
            ValidarEstadoEditable(pedido.Estado);

            pedido.Modalidad = modalidad;
            pedido.NumeroMesa = modalidad == ModalidadPedido.ParaLlevar ? null : numeroMesa;

            _logger.LogInformation("🏷️ Modalidad del pedido #{PedidoId} actualizada a {Modalidad}", pedidoId, modalidad);
            return pedido;
        }

        public async Task<PedidoDto> ActualizarClienteAsync(int pedidoId, string cliente)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);
            ValidarEstadoEditable(pedido.Estado);

            pedido.Cliente = cliente?.Trim();
            _logger.LogInformation("👤 Cliente del pedido #{PedidoId} actualizado a {Cliente}", pedidoId, cliente);

            return pedido;
        }

        public async Task<bool> ValidarPedidoParaGuardarAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            var errores = new List<string>();

            if (!pedido.Items.Any())
                errores.Add("Debe tener al menos un producto");

            if (string.IsNullOrWhiteSpace(pedido.Cliente))
                errores.Add("Debe tener un cliente asignado");

            if (pedido.Modalidad == ModalidadPedido.EnMesa && (!pedido.NumeroMesa.HasValue || pedido.NumeroMesa <= 0))
                errores.Add("Debe tener un número de mesa válido para pedidos en mesa");

            if (errores.Any())
            {
                _logger.LogWarning("❌ Validación fallida para pedido #{PedidoId}: {Errores}", pedidoId, string.Join(", ", errores));
                return false;
            }

            return true;
        }

        public async Task<ServiceResult> GuardarPedidoAsync(int idPedido)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            _logger.LogInformation("💾 Guardando pedido #{PedidoId}", idPedido);

            var esValido = await ValidarPedidoParaGuardarAsync(idPedido);
            if (!esValido)
            {
                return ServiceResult.Error("No se puede guardar el pedido. Verifique los datos requeridos");
            }

            // Transición de estado: Borrador → Pendiente
            await CambiarEstadoPedidoAsync(pedido, PedidoEstado.Pendiente, "Pedido guardado y listo para facturación");

            _logger.LogInformation("✅ Pedido #{PedidoId} guardado exitosamente", idPedido);
            return ServiceResult.SuccessResult($"Pedido #{pedido.Id_Venta} guardado correctamente. Proceda al pago.", pedido);
        }

        public async Task<ServiceResult> IniciarPreparacionAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            _logger.LogInformation("👨‍🍳 Iniciando preparación del pedido #{PedidoId}", pedidoId);

            if (pedido.Estado != PedidoEstado.Pendiente && pedido.Estado != PedidoEstado.Borrador)
            {
                return ServiceResult.Error("Solo se pueden iniciar preparación de pedidos pendientes o en borrador");
            }

            // Transición de estado: Pendiente/Borrador → EnPreparacion
            await CambiarEstadoPedidoAsync(pedido, PedidoEstado.EnPreparacion, "Preparación iniciada en cocina");

            _logger.LogInformation("✅ Preparación del pedido #{PedidoId} iniciada", pedidoId);
            return ServiceResult.SuccessResult("Preparación del pedido iniciada");
        }

        public async Task<ServiceResult> MarcarComoListoAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            _logger.LogInformation("🔔 Marcando pedido #{PedidoId} como listo", pedidoId);

            if (pedido.Estado != PedidoEstado.EnPreparacion)
            {
                return ServiceResult.Error("Solo se pueden marcar como listo pedidos en preparación");
            }

            // Transición de estado: EnPreparacion → Listo
            await CambiarEstadoPedidoAsync(pedido, PedidoEstado.Listo, "Pedido listo para entregar");

            _logger.LogInformation("✅ Pedido #{PedidoId} marcado como listo", pedidoId);
            return ServiceResult.SuccessResult("Pedido marcado como listo para entregar");
        }

        public async Task<ServiceResult> MarcarComoEntregadoAsync(int pedidoId)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            if (pedido.Estado != PedidoEstado.Listo)
                return ServiceResult.Error("Solo se pueden marcar como entregado pedidos listos");

            // Transición de estado: Listo → Entregado
            await CambiarEstadoPedidoAsync(pedido, PedidoEstado.Entregado, "Pedido entregado al cliente");

            _logger.LogInformation("📦 Pedido #{PedidoId} marcado como entregado", pedidoId);
            return ServiceResult.SuccessResult("Pedido marcado como entregado correctamente");
        }

        public async Task<ServiceResult> CancelarPedidoAsync(int pedidoId, string razon)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);

            _logger.LogInformation("🚫 Cancelando pedido #{PedidoId}", pedidoId);

            if (pedido.Estado == PedidoEstado.Entregado)
            {
                return ServiceResult.Error("No se puede cancelar un pedido ya entregado");
            }

            if (pedido.Estado == PedidoEstado.Cancelado)
            {
                return ServiceResult.Error("El pedido ya está cancelado");
            }

            // Transición a estado cancelado
            await CambiarEstadoPedidoAsync(pedido, PedidoEstado.Cancelado, $"Pedido cancelado: {razon}");
            pedido.MotivoCancelacion = razon;

            _logger.LogInformation("✅ Pedido #{PedidoId} cancelado: {Razon}", pedidoId, razon);
            return ServiceResult.SuccessResult("Pedido cancelado correctamente");
        }

        // MÉTODOS DE CONSULTA
        public Task<List<PedidoDto>> ObtenerTodosLosPedidosAsync(int idUsuario)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario)
                .OrderByDescending(p => p.Fecha)
                .ToList();

            return Task.FromResult(pedidos);
        }

        public async Task<bool> PuedeEditarseAsync(int idPedido)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);
            return pedido.Estado == PedidoEstado.Borrador;
        }

        public async Task ActualizarPedidoConFactura(int pedidoId, int facturaId, string numeroFactura)
        {
            if (_pedidosTemporales.TryGetValue(pedidoId, out var pedido))
            {
                pedido.FacturaId = facturaId;
                pedido.NumeroFactura = numeroFactura;

                // Si el pedido estaba pendiente o borrador y se facturó, puede pasar a preparación automáticamente
                if (pedido.Estado == PedidoEstado.Pendiente || pedido.Estado == PedidoEstado.Borrador)
                {
                    await CambiarEstadoPedidoAsync(pedido, PedidoEstado.EnPreparacion, "Factura generada - En preparación");
                }

                _logger.LogInformation("🧾 Pedido {PedidoId} actualizado con Factura {FacturaId}", pedidoId, facturaId);
            }
        }

        public Task<List<PedidoDto>> ObtenerPedidosParaCocinaAsync()
        {
            var pedidosCocina = _pedidosTemporales.Values
                .Where(p => p.Estado == PedidoEstado.Pendiente || p.Estado == PedidoEstado.EnPreparacion)
                .OrderBy(p => p.Fecha)
                .ToList();

            return Task.FromResult(pedidosCocina);
        }

        public async Task<ServiceResult> GuardarComoBorradorAsync(int idPedido)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);
            
            if (pedido.Estado != PedidoEstado.Borrador)
            {
                 return ServiceResult.Error("El pedido ya no está en estado borrador");
            }

            _logger.LogInformation("💾 Pedido #{PedidoId} guardado como borrador", idPedido);
            
            return ServiceResult.SuccessResult("Borrador guardado correctamente");
        }

        public async Task<ServiceResult> AgregarNotaCocinaAsync(int pedidoId, string nota)
        {
            var pedido = await ObtenerPedidoAsync(pedidoId);
            
            if (pedido.Estado == PedidoEstado.Entregado || pedido.Estado == PedidoEstado.Cancelado)
                 return ServiceResult.Error("No se puede agregar nota a un pedido finalizado");

            pedido.Notas = nota;
            _logger.LogInformation("📝 Nota agregada al pedido #{PedidoId}: {Nota}", pedidoId, nota);
            
            return ServiceResult.SuccessResult("Nota agregada correctamente");
        }

        public Task<ResumenPedidosDto> ObtenerResumenPedidosAsync(int usuarioId)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == usuarioId)
                .ToList();

            var resumen = new ResumenPedidosDto
            {
                TotalBorrador = pedidos.Count(p => p.Estado == PedidoEstado.Borrador),
                TotalEnCocina = pedidos.Count(p => p.Estado == PedidoEstado.Pendiente || p.Estado == PedidoEstado.EnPreparacion),
                TotalListos = pedidos.Count(p => p.Estado == PedidoEstado.Listo),
                TotalEntregados = pedidos.Count(p => p.Estado == PedidoEstado.Entregado),
                TotalCancelados = pedidos.Count(p => p.Estado == PedidoEstado.Cancelado)
            };

            return Task.FromResult(resumen);
        }

        // MÉTODOS PRIVADOS AUXILIARES
        private void ValidarEstadoEditable(PedidoEstado estado)
        {
            if (estado != PedidoEstado.Borrador)
                throw new InvalidOperationException("No se puede modificar un pedido que no está en estado borrador");
        }

        private void RecalcularTotal(PedidoDto pedido)
        {
            pedido.Total = pedido.Items.Sum(item => item.Subtotal);
        }

        private async Task CambiarEstadoPedidoAsync(PedidoDto pedido, PedidoEstado nuevoEstado, string observacion)
        {
            var estadoAnterior = pedido.Estado;
            pedido.Estado = nuevoEstado;
            pedido.FechaActualizacion = DateTime.Now;

            // Registrar en historial
            pedido.HistorialEstados ??= new List<EstadoPedidoDto>();
            pedido.HistorialEstados.Add(new EstadoPedidoDto
            {
                Estado = nuevoEstado,
                Fecha = DateTime.Now,
                Observacion = observacion
            });

            _logger.LogInformation("🔄 Pedido #{PedidoId} cambió de {EstadoAnterior} a {EstadoNuevo}",
                pedido.Id_Venta, estadoAnterior, nuevoEstado);
        }

        public Task VerificarEstadoPedidosAsync(int usuarioId)
        {
            var pedidosUsuario = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == usuarioId)
                .ToList();

            _logger.LogInformation("📊 Resumen pedidos usuario {UsuarioId}: {Count} pedidos",
                usuarioId, pedidosUsuario.Count);

            return Task.CompletedTask;
        }
    }
}