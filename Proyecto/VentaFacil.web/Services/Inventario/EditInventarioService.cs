using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Inventario;

namespace VentaFacil.web.Services.Inventario
{
    public class EditInventarioService : IEditInventarioService
    {
        private readonly ApplicationDbContext _context;

        public EditInventarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EditInventarioResponse> EditarAsync(InventarioDto inventarioDto)
        {
            var response = new EditInventarioResponse();

            try
            {
                var inventario = await _context.Inventario
                    .FirstOrDefaultAsync(i => i.Id_Inventario == inventarioDto.Id_Inventario);

                if (inventario == null)
                {
                    response.Success = false;
                    response.Message = "Inventario no encontrado.";
                    return response;
                }

                // Actualizar campos
                inventario.Id_Producto = inventarioDto.Id_Producto;
                inventario.StockActual = inventarioDto.StockActual;

                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Inventario editado correctamente.";
                response.InventarioId = inventario.Id_Inventario;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al editar inventario: {ex.Message}";
            }

            return response;
        }
    }
}
