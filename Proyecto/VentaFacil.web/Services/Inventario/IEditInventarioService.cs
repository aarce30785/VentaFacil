using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Inventario;

namespace VentaFacil.web.Services.Inventario
{
    public interface IEditInventarioService
    {
        Task<EditInventarioResponse> EditarAsync(InventarioDto inventarioDto);
    }
}
