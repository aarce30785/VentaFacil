using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enums;
using VentaFacil.web.Services.Producto;

namespace VentaFacil.web.Services.Pedido
{
    /// <summary>
    /// Servicio en memoria para PE3 (Sprint 1).
    /// Centraliza reglas de edición, eliminación y recálculo.
    /// </summary>
    public class PedidoService : IPedidoService
    {
        // “Storage” en memoria por ahora (clave: Id_Venta)
        private static readonly ConcurrentDictionary<int, PedidoDto> _store = new();

        private readonly IProductoService _productoService;

        public PedidoService(IProductoService productoService)
        {
            _productoService = productoService;
        }

        public Task<PedidoDto> ObtenerAsync(int id)
        {
            if (!_store.TryGetValue(id, out var pedido))
            {
                // Si no existe, crea uno borrador para pruebas
                pedido = new PedidoDto
                {
                    Id_Venta = id,
                    Estado = PedidoEstado.Borrador,
                    Fecha = DateTime.Now
                };

                // Simulación Sprint 1: nombres para los 2 pedidos de demo
                if (id == 1)
                {
                    pedido.Cliente = "Ana López";
                    // (opcional) si quieres que aparezca algo en la tabla al entrar, descomenta:
                    // pedido.Items.Add(new PedidoItemDto
                    // {
                    //     Id_Detalle = 1,
                    //     Id_Producto = 1,
                    //     NombreProducto = "Hamburguesa Clásica",
                    //     PrecioUnitario = 3200m,
                    //     Cantidad = 1
                    // });
                }
                else if (id == 2)
                {
                    pedido.Cliente = "Pedro Ruiz";
                }

                // Recalcula total por si sembraste ítems arriba
                pedido.Total = pedido.Items.Sum(i => i.Subtotal);

                _store[id] = pedido;
            }
            return Task.FromResult(pedido);
        }

        public Task<PedidoDto> GuardarCabeceraAsync(PedidoDto dto)
        {
            var pedido = _store.GetOrAdd(dto.Id_Venta, dto);

            AsegurarEditable(pedido);

            pedido.Cliente = dto.Cliente?.Trim();
            pedido.Id_Usuario = dto.Id_Usuario;
            pedido.Modalidad = dto.Modalidad;
            pedido.NumeroMesa = dto.Modalidad == 1 ? dto.NumeroMesa : null;

            RecalcularTotales(pedido);
            return Task.FromResult(pedido);
        }

        public async Task<PedidoDto> AgregarItemAsync(int pedidoId, int productoId, int cantidad)
        {
            var pedido = await ObtenerAsync(pedidoId);
            AsegurarEditable(pedido);

            var prod = await _productoService.ObtenerPorIdAsync(productoId)
                       ?? throw new InvalidOperationException("Producto no encontrado.");

            var item = pedido.Items.FirstOrDefault(i => i.Id_Producto == productoId);
            if (item is null)
            {
                item = new PedidoItemDto
                {
                    Id_Detalle = pedido.Items.Count == 0 ? 1 : pedido.Items.Max(i => i.Id_Detalle) + 1,
                    Id_Producto = prod.Id_Producto,
                    NombreProducto = prod.Nombre ?? "Producto",
                    PrecioUnitario = prod.Precio,
                    Cantidad = 0
                };
                pedido.Items.Add(item);
            }

            item.Cantidad += Math.Max(1, cantidad);
            // Subtotal es calculado en el DTO: no se asigna aquí

            RecalcularTotales(pedido);
            return pedido;
        }

        public async Task<PedidoDto> ActualizarCantidadAsync(int pedidoId, int itemId, int cantidad)
        {
            var pedido = await ObtenerAsync(pedidoId);
            AsegurarEditable(pedido);

            var item = pedido.Items.FirstOrDefault(i => i.Id_Detalle == itemId)
                       ?? throw new InvalidOperationException("Ítem no encontrado.");

            if (cantidad <= 0)
            {
                pedido.Items.Remove(item);
            }
            else
            {
                item.Cantidad = cantidad;
                // Subtotal es calculado en el DTO
            }

            RecalcularTotales(pedido);
            return pedido;
        }

        public async Task<PedidoDto> EliminarItemAsync(int pedidoId, int itemId)
        {
            var pedido = await ObtenerAsync(pedidoId);
            AsegurarEditable(pedido);

            var item = pedido.Items.FirstOrDefault(i => i.Id_Detalle == itemId);
            if (item != null)
            {
                pedido.Items.Remove(item);
            }

            RecalcularTotales(pedido);
            return pedido;
        }

        // ====== Reglas y utilidades ======

        private static void AsegurarEditable(PedidoDto pedido)
        {
            if (pedido.Estado != PedidoEstado.Borrador)
                throw new InvalidOperationException("Este pedido no se puede modificar (no está en Borrador).");
        }

        private static void RecalcularTotales(PedidoDto pedido)
        {
            var subtotal = pedido.Items.Sum(i => i.Subtotal); // usa tu Subtotal calculado
            // Si luego agregan impuestos/servicio, aplíquenlos aquí
            pedido.Total = subtotal;
        }
    }
}
