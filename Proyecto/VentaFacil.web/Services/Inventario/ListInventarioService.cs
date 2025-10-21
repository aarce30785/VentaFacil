using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Inventario;

namespace VentaFacil.web.Services.Inventario
{
    public class ListInventarioService : IListInventarioService
    {
        private readonly ApplicationDbContext _context;

        public ListInventarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ListInventarioResponse> ListarTodosAsync()
        {
            var response = new ListInventarioResponse();
            try
            {
                var inventarios = await _context.Inventario
                    .AsNoTracking()
                    .ToListAsync();

                response.Success = true;
                response.Inventarios = inventarios.Select(i => new InventarioDto
                {
                    Id_Inventario = i.Id_Inventario,
                    Id_Producto = i.Id_Producto,
                    StockActual = i.StockActual
                }).ToList();

                response.Message = response.Inventarios.Count > 0
                    ? "Inventarios encontrados."
                    : "No hay inventarios registrados.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error al listar inventarios: {ex.Message}";
            }

            return response;
        }
    }
}
