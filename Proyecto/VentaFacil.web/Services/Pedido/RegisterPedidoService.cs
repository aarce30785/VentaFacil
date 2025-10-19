using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Pedido;

namespace VentaFacil.web.Services.Pedido
{
    public class RegisterPedidoService : IRegisterPedidoService
    {
        private readonly ApplicationDbContext _context;

        public RegisterPedidoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CreatePedidoResponse> RegisterAsync(PedidoDto pedido)
        {
            var response = new CreatePedidoResponse();

            try
            {
                // Validar que haya al menos un producto
                if (pedido.Items == null || !pedido.Items.Any())
                {
                    response.Success = false;
                    response.Message = "Debe agregarse al menos un producto al pedido.";
                    return response;
                }

                // Crear la venta (estado pendiente = false)
                var venta = new Models.Venta
                {
                    Fecha = DateTime.Now,
                    Total = pedido.Items.Sum(i => (i.Cantidad * i.PrecioUnitario) - i.Descuento),
                    Estado = false,
                    Id_Usuario = pedido.Id_Usuario
                };

                _context.Add(venta);
                await _context.SaveChangesAsync();

                // Crear detalle por cada producto
                foreach (var item in pedido.Items)
                {
                    var detalle = new Models.DetalleVenta
                    {
                        Id_Venta = venta.Id_Venta,
                        Id_Producto = item.Id_Producto,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        Descuento = item.Descuento
                    };
                    _context.Add(detalle);
                }

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Pedido registrado correctamente.";
                response.PedidoId = venta.Id_Venta;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al registrar pedido: {ex.Message}";
            }

            return response;
        }
    }
}
