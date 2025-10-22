using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Producto;

namespace VentaFacil.web.Services.Producto
{
    public class ProductoService : IProductoService
    {
        private readonly ApplicationDbContext _context;

        public ProductoService(ApplicationDbContext context)
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

        public async Task<RegisterProductoResponse> RegisterAsync(ProductoDto productoDto)
        {
            var response = new RegisterProductoResponse();

            try
            {
                // Validación básica: nombre único y estado activo
                var exist = await _context.Producto
                    .AnyAsync(p => p.Nombre == productoDto.Nombre && p.Estado);

                if (exist)
                {
                    response.Success = false;
                    response.Message = "Ya existe un producto activo con ese nombre.";
                    return response;
                }

                productoDto.Estado = true; // Por defecto, activo

                // Mapear ProductoDto a Producto
                var producto = new Models.Producto
                {
                    Nombre = productoDto.Nombre,
                    Descripcion = productoDto.Descripcion,
                    Precio = productoDto.Precio,
                    Imagen = productoDto.Imagen,
                    StockMinimo = productoDto.StockMinimo,
                    Estado = productoDto.Estado,
                    Id_Categoria = productoDto.Id_Categoria
                };

                _context.Producto.Add(producto);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Producto registrado correctamente.";
                response.ProductoId = producto.Id_Producto;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al registrar producto: {ex.Message}";
            }

            return response;
        }

        public async Task<EditProductoResponse> EditarAsync(ProductoDto productoDto)
        {
            var response = new EditProductoResponse();

            try
            {
                var producto = await _context.Producto
                    .FirstOrDefaultAsync(p => p.Id_Producto == productoDto.Id_Producto);

                if (producto == null)
                {
                    response.Success = false;
                    response.Message = "Producto no encontrado.";
                    return response;
                }

                // Validación de unicidad de nombre 
                var existeNombre = await _context.Producto
                    .AnyAsync(p => p.Nombre == productoDto.Nombre && p.Id_Producto != productoDto.Id_Producto && p.Estado);

                if (existeNombre)
                {
                    response.Success = false;
                    response.Message = "Ya existe otro producto activo con ese nombre.";
                    return response;
                }

                // Actualizar campos
                producto.Nombre = productoDto.Nombre;
                producto.Descripcion = productoDto.Descripcion;
                producto.Precio = productoDto.Precio;
                producto.Imagen = productoDto.Imagen;
                producto.StockMinimo = productoDto.StockMinimo;
                producto.Estado = productoDto.Estado;
                producto.Id_Categoria = productoDto.Id_Categoria;

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Producto editado correctamente.";
                response.ProductoId = producto.Id_Producto;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al editar producto: {ex.Message}";
            }

            return response;
        }

    }
}
