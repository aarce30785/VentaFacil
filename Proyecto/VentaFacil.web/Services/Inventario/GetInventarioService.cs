using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Inventario;

namespace VentaFacil.web.Services.Inventario
{
    public class GetInventarioService : IGetInventarioService
    {
        private readonly ApplicationDbContext _context;

        public GetInventarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GetInventarioResponse> GetByIdAsync(int id)
        {
            var response = new GetInventarioResponse();

            try
            {
                var inventario = await _context.Inventario
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id_Inventario == id);

                if (inventario == null)
                {
                    response.Success = false;
                    response.Message = "Inventario no encontrado.";
                    return response;
                }

                response.Success = true;
                response.Inventario = new InventarioDto
                {
                    Id_Inventario = inventario.Id_Inventario,
                    Id_Producto = inventario.Id_Producto,
                    StockActual = inventario.StockActual
                };
                response.Message = "Inventario encontrado.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al buscar inventario: {ex.Message}";
            }

            return response;
        }
    }
}