using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;

namespace VentaFacil.web.Services.Pedido
{
    public interface IPedidoService
    {
        Task<PedidoDto> CrearPedidoAsync(int idUsuario, string? cliente = null);
        Task<PedidoDto> ObtenerPedidoAsync(int idPedido);
        Task<PedidoDto> AgregarProductoAsync(int idPedido, int idProducto, int cantidad);
        Task<PedidoDto> ActualizarCantidadProductoAsync(int idPedido, int idDetalle, int cantidad);
        Task<PedidoDto> EliminarProductoAsync(int idPedido, int idDetalle);
        Task<PedidoDto> ActualizarModalidadAsync(int idPedido, ModalidadPedido modalidad, int? numeroMesa = null);
        Task<PedidoDto> ActualizarClienteAsync(int pedidoId, string cliente);
        Task<ServiceResult> GuardarPedidoAsync(int idPedido);
        Task<ServiceResult> GuardarComoBorradorAsync(int idPedido);
        Task<bool> PuedeEditarseAsync(int idPedido);
        Task<List<PedidoDto>> ObtenerTodosLosPedidosAsync(int idUsuario);
        Task<bool> ValidarPedidoParaGuardarAsync(int pedidoId);
        Task<List<PedidoDto>> ObtenerPedidosParaCocinaAsync();
        Task<ServiceResult> MarcarComoListoAsync(int pedidoId);
        Task<ServiceResult> AgregarNotaCocinaAsync(int pedidoId, string nota);
        Task<ServiceResult> CancelarPedidoAsync(int pedidoId, string razon);
        Task<ServiceResult> MarcarComoEntregadoAsync(int pedidoId);
        Task<ServiceResult> IniciarPreparacionAsync(int pedidoId);

        Task ActualizarPedidoConFactura(int ventaId, int id_Factura, string numeroFactura);
        Task<ResumenPedidosDto> ObtenerResumenPedidosAsync(int usuarioId);
    }

   
}
