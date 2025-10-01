using System.Threading.Tasks;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Producto;

namespace VentaFacil.web.Services.Producto
{
    public interface IRegisterProductoService
    {
        Task<RegisterProductoResponse> RegisterAsync(ProductoDto productoDto);
    }
}