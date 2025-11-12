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
        Task<ResultadoPedido> GuardarPedidoAsync(int idPedido);
        Task<ResultadoPedido> GuardarComoBorradorAsync(int idPedido);
        Task<bool> PuedeEditarseAsync(int idPedido);
        Task<List<PedidoDto>> ObtenerPedidosBorradorAsync(int idUsuario);
        Task<List<PedidoDto>> BuscarPedidosAsync(int idUsuario, string criterio);
        Task<ResultadoPedido> CancelarPedidoAsync(int idPedido, string motivoCancelacion);

        Task<List<PedidoDto>> ObtenerPedidosPendientesAsync(int idUsuario);
        Task<List<PedidoDto>> ObtenerTodosLosPedidosAsync(int idUsuario);
    }

    public class ResultadoPedido
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PedidoDto? Pedido { get; set; }
    }
}