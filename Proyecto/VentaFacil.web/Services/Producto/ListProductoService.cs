using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Producto;

namespace VentaFacil.web.Services.Producto
{
    public class ListProductoService : IListProductoService
    {
        private readonly ApplicationDbContext _context;

        public ListProductoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ListProductoResponse> ListarTodosAsync()
        {
            var response = new ListProductoResponse();
            try
            {
                var productos = await _context.Producto
                    .AsNoTracking()
                    .Where(p => p.Estado)
                    .ToListAsync();

                response.Success = true;
                response.Productos = productos.Select(p => new ProductoDto
                {
                    Id_Producto = p.Id_Producto,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    Imagen = p.Imagen,
                    StockMinimo = p.StockMinimo,
                    Estado = p.Estado,
                    Id_Categoria = p.Id_Categoria
                }).ToList();

                response.Message = response.Productos.Count > 0
                    ? "Productos encontrados."
                    : "No hay productos registrados.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al listar productos: {ex.Message}";
            }

            return response;
        }
    }
}