using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Enums;
using VentaFacil.web.Services.Producto;

namespace VentaFacil.web.Services.Pedido
{
    public class PedidoService : IPedidoService
    {
        private static readonly ConcurrentDictionary<int, PedidoDto> _pedidosTemporales = new();
        private static int _contadorId = 1;
        private static readonly object _lock = new object();
        private readonly IProductoService _productoService;

        public PedidoService(IProductoService productoService)
        {
            _productoService = productoService;
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

            // Verificar si el producto ya existe en el pedido
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

        public async Task<PedidoDto> ActualizarModalidadAsync(int idPedido, ModalidadPedido modalidad, int? numeroMesa = null)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            if (!await PuedeEditarseAsync(idPedido))
                throw new InvalidOperationException("No se puede modificar un pedido que no está en estado borrador");

            pedido.Modalidad = modalidad;
            pedido.NumeroMesa = modalidad == ModalidadPedido.EnMesa ? numeroMesa : null;

            return pedido;
        }

        public async Task<ResultadoPedido> GuardarPedidoAsync(int idPedido)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            // Validación: Solo se puede enviar a cocina si está en Borrador (PE04 Requisito b)
            if (pedido.Estado != PedidoEstado.Borrador)
            {
                return new ResultadoPedido
                {
                    Success = false,
                    Message = $"El pedido #{idPedido} ya no está en estado borrador. Su estado actual es: {pedido.Estado}. Solo los pedidos en borrador pueden ser enviados a cocina."
                };
            }

            // Validaciones de datos obligatorios (PE04 Requisito c)
            var validacion = ValidarPedidoParaEnvio(pedido);
            if (!validacion.Success)
            {
                return validacion;
            }

            // PE04 Escenario 1: Registrar como enviado a cocina
            pedido.Estado = PedidoEstado.EnviadoACocina;
            pedido.Fecha = DateTime.Now;

            // Aquí en el futuro persistiríamos a la base de datos

            return new ResultadoPedido
            {
                Success = true,
                Message = $"Pedido #{pedido.Id_Venta} enviado a cocina correctamente y marcado como '{nameof(PedidoEstado.EnviadoACocina)}'.",
                Pedido = pedido
            };
        }

        private ResultadoPedido ValidarPedidoParaEnvio(PedidoDto pedido)
        {
            // Validaciones según PE1 Escenario 2
            if (!pedido.Items.Any())
            {
                return new ResultadoPedido
                {
                    Success = false,
                    Message = "Debe agregar al menos un producto al pedido"
                };
            }

            // Validaciones según PE2 Escenario 2
            if (pedido.Modalidad == ModalidadPedido.EnMesa && !pedido.NumeroMesa.HasValue)
            {
                return new ResultadoPedido
                {
                    Success = false,
                    Message = "Debe ingresar un número de mesa para pedidos en mesa"
                };
            }

            return new ResultadoPedido { Success = true };
        }

        public async Task<ResultadoPedido> GuardarComoBorradorAsync(int idPedido)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            // PE1 Escenario 3: Conservar como borrador
            pedido.Estado = PedidoEstado.Borrador;
            pedido.Fecha = DateTime.Now;

            return new ResultadoPedido
            {
                Success = true,
                Message = $"Pedido #{pedido.Id_Venta} guardado como borrador correctamente",
                Pedido = pedido
            };
        }

        public async Task<ResultadoPedido> CancelarPedidoAsync(int idPedido, string motivoCancelacion)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            // Validar estados no permitidos para cancelación
            if (pedido.Estado == PedidoEstado.Entregado)
            {
                return new ResultadoPedido
                {
                    Success = false,
                    Message = "No es posible cancelar un pedido que ya ha sido entregado."
                };
            }

            if (pedido.Estado == PedidoEstado.EnPreparacion || pedido.Estado == PedidoEstado.Listo || pedido.Estado == PedidoEstado.Cancelado)
            {
                return new ResultadoPedido
                {
                    Success = false,
                    Message = "No es posible cancelar un pedido que ya está en preparación o finalizado."
                };
            }

            // Estados permitidos: Borrador (0) o EnviadoACocina (1)
            pedido.Estado = PedidoEstado.Cancelado;
            pedido.Fecha = DateTime.Now;
            pedido.MotivoCancelacion = string.IsNullOrWhiteSpace(motivoCancelacion) ? "Cancelado sin motivo especificado" : motivoCancelacion;

            return new ResultadoPedido
            {
                Success = true,
                Message = $"Pedido #{pedido.Id_Venta} cancelado correctamente.",
                Pedido = pedido
            };
        }

        public async Task<bool> PuedeEditarseAsync(int idPedido)
        {
            var pedido = await ObtenerPedidoAsync(idPedido);

            return pedido.Estado == PedidoEstado.Borrador;
        }

        public Task<List<PedidoDto>> BuscarPedidosAsync(int idUsuario, string criterio)
        {
            var pedidosDelUsuario = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario);

            if (string.IsNullOrWhiteSpace(criterio))
            {
                return Task.FromResult(new List<PedidoDto>());
            }

            var criterioLimpio = criterio.Trim();
            List<PedidoDto> resultados;

            if (int.TryParse(criterioLimpio, out int idCriterio))
            {
                resultados = pedidosDelUsuario
                    .Where(p => p.Id_Venta == idCriterio)
                    .OrderByDescending(p => p.Fecha)
                    .ToList();
            }
            else
            {
                resultados = pedidosDelUsuario
                    .Where(p => p.Cliente != null && p.Cliente.IndexOf(criterioLimpio, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderByDescending(p => p.Fecha)
                    .ToList();
            }

            return Task.FromResult(resultados);
        }

        public Task<List<PedidoDto>> ObtenerPedidosBorradorAsync(int idUsuario)
        {
            var pedidos = _pedidosTemporales.Values
                .Where(p => p.Id_Usuario == idUsuario && p.Estado == PedidoEstado.Borrador)
                .OrderByDescending(p => p.Fecha)
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
                .Where(p => p.Id_Usuario == idUsuario && p.Estado == PedidoEstado.EnviadoACocina)
                .OrderByDescending(p => p.Fecha)
                .ToList();

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
    }
}