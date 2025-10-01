using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Producto;

namespace VentaFacil.web.Services.Producto
{
    public interface IEditProductoService
    {
        Task<EditProductoResponse> EditarAsync(ProductoDto productoDto);
    }
}