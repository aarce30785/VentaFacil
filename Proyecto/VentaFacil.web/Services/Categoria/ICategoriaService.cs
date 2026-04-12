using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;

public interface ICategoriaService
{
    Task<List<CategoriaDto>> ListarTodasAsync();
    Task<CategoriaDto?> ObtenerPorIdAsync(int id);
    Task<bool> CrearAsync(CategoriaDto dto);
    Task<bool> EditarAsync(CategoriaDto dto);
    Task<bool> EliminarAsync(int id);
}