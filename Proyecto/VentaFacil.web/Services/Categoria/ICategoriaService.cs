using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;

public interface ICategoriaService
{
    Task<List<CategoriaDto>> ListarTodasAsync();
}