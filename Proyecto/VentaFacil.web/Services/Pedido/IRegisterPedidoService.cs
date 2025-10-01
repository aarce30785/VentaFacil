using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Pedido;

namespace VentaFacil.web.Services.Pedido
{
    public interface IRegisterPedidoService
    {
        Task<CreatePedidoResponse> RegisterAsync(PedidoDto pedido);
    }
}
