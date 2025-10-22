using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.Producto
{
    public interface IProductoService
    {
        Task<List<ProductoDto>> ObtenerTodosAsync();
        Task<ProductoDto?> ObtenerPorIdAsync(int id);
    }
}
