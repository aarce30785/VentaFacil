using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Producto;

namespace VentaFacil.web.Services.Producto
{
    public class RegisterProductoService : IRegisterProductoService
    {
        private readonly ApplicationDbContext _context;

        public RegisterProductoService(ApplicationDbContext context)
        {
            _context = context;
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
    }
}
