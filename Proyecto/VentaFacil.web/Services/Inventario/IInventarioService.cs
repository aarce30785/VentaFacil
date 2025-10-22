using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.Inventario
{
    public interface IInventarioService
    {
        Task<List<InventarioDto>> ListarTodosAsync();
        Task<InventarioDto?> GetByIdAsync(int id);
        Task<bool> RegistrarAsync(InventarioDto dto);
        Task<bool> EditarAsync(InventarioDto dto);
        Task<bool> EliminarAsync(int id);
        Task<bool> AgregarUnidadAsync(int id);
        Task<bool> QuitarUnidadAsync(int id);
    }
}
