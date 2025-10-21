using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Inventario;

namespace VentaFacil.web.Services.Inventario
{
    public interface IRegisterInventarioService
    {
        Task<RegisterInventarioResponse> RegisterAsync(InventarioDto inventarioDto);
    }
}