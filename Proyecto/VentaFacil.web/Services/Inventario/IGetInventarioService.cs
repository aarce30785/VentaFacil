using System.Threading.Tasks;
using VentaFacil.web.Models.Response.Inventario;

namespace VentaFacil.web.Services.Inventario
{
    public interface IGetInventarioService
    {
        Task<GetInventarioResponse> GetByIdAsync(int id);
    }
}
