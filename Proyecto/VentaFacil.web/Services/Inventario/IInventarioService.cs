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
        Task<bool> EditarAsync(InventarioDto dto, int idUsuario);
        Task<bool> EliminarAsync(int id);
        Task<bool> AgregarUnidadAsync(int id, int idUsuario);
        Task<bool> QuitarUnidadAsync(int id, int idUsuario);
        Task<bool> RegistrarEntradaAsync(int idInventario, int cantidad, string observaciones, int idUsuario);
        Task<bool> RegistrarSalidaAsync(int idInventario, int cantidad, string observaciones, int idUsuario);
        Task<List<InventarioDto>> ObtenerStockMinimoAsync();
    }
}
