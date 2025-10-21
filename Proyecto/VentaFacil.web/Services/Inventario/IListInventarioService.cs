using System.Threading.Tasks;
using VentaFacil.web.Models.Response.Inventario;

namespace VentaFacil.web.Services.Inventario
{
    public interface IListInventarioService
    {
        Task<ListInventarioResponse> ListarTodosAsync();
    }
}
