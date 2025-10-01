using System.Threading.Tasks;
using VentaFacil.web.Models.Response.Producto;

namespace VentaFacil.web.Services.Producto
{
    public interface IListProductoService
    {
        Task<ListProductoResponse> ListarTodosAsync();
    }
}