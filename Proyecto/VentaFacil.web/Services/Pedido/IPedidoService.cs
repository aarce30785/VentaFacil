using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.Pedido
{
    public interface IPedidoService
    {
        Task<PedidoDto> ObtenerAsync(int id);
        Task<PedidoDto> GuardarCabeceraAsync(PedidoDto dto);
        Task<PedidoDto> AgregarItemAsync(int pedidoId, int productoId, int cantidad);
        Task<PedidoDto> ActualizarCantidadAsync(int pedidoId, int itemId, int cantidad);
        Task<PedidoDto> EliminarItemAsync(int pedidoId, int itemId);
    }
}
