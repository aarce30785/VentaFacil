using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.Categoria
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ApplicationDbContext _context;

        public CategoriaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoriaDto>> ListarTodasAsync()
        {
            return await _context.Categoria
                .Select(c => new CategoriaDto
                {
                    Id_Categoria = c.Id_Categoria,
                    Nombre = c.Nombre
                })
                .ToListAsync();
        }
    }
}
