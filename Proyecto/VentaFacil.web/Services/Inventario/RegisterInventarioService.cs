using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Inventario;

namespace VentaFacil.web.Services.Inventario
{
    public class RegisterInventarioService : IRegisterInventarioService
    {
        private readonly ApplicationDbContext _context;

        public RegisterInventarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RegisterInventarioResponse> RegisterAsync(InventarioDto inventarioDto)
        {
            var response = new RegisterInventarioResponse();

            try
            {
                // Validación: solo un inventario por producto
                var exist = await _context.Inventario
                    .AnyAsync(i => i.Id_Producto == inventarioDto.Id_Producto);

                if (exist)
                {
                    response.Success = false;
                    response.Message = "Ya existe un inventario para este producto.";
                    return response;
                }

                // Mapear InventarioDto a Inventario
                var inventario = new Models.Inventario
                {
                    Id_Producto = inventarioDto.Id_Producto,
                    StockActual = inventarioDto.StockActual
                };

                _context.Inventario.Add(inventario);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Inventario registrado correctamente.";
                response.InventarioId = inventario.Id_Inventario;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al registrar inventario: {ex.Message}";
            }

            return response;
        }
    }
}
