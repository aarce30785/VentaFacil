using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Producto;

namespace VentaFacil.web.Services.Producto
{
    public class EditProductoService : IEditProductoService
    {
        private readonly ApplicationDbContext _context;

        public EditProductoService(ApplicationDbContext context)
        {
            _context = context;
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