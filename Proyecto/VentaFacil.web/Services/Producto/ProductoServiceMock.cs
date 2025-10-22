using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.Producto
{
    public class ProductoServiceMock : IProductoService
    {
        private static readonly List<ProductoDto> _data = new()
        {
            new ProductoDto { Id_Producto = 1, Nombre = "Hamburguesa Clásica", Precio = 3200m },
            new ProductoDto { Id_Producto = 2, Nombre = "Papas Fritas",        Precio = 1500m },
            new ProductoDto { Id_Producto = 3, Nombre = "Refresco 350ml",      Precio = 1200m },
            new ProductoDto { Id_Producto = 4, Nombre = "Café Americano",      Precio = 1000m },
            new ProductoDto { Id_Producto = 5, Nombre = "Postre del Día",      Precio = 1800m },
        };

        public Task<List<ProductoDto>> ObtenerTodosAsync()
        {
            return Task.FromResult(_data.ToList());
        }

        public Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            var producto = _data.FirstOrDefault(p => p.Id_Producto == id);
            return Task.FromResult(producto);
        }
    }
}
