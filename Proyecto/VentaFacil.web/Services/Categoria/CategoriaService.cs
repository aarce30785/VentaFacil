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
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion
                })
                .ToListAsync();
        }

        public async Task<CategoriaDto?> ObtenerPorIdAsync(int id)
        {
            return await _context.Categoria
                .Where(c => c.Id_Categoria == id)
                .Select(c => new CategoriaDto
                {
                    Id_Categoria = c.Id_Categoria,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CrearAsync(CategoriaDto dto)
        {
            var categoria = new Models.Categoria
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion
            };

            _context.Categoria.Add(categoria);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EditarAsync(CategoriaDto dto)
        {
            var categoria = await _context.Categoria.FindAsync(dto.Id_Categoria);
            if (categoria == null) return false;

            categoria.Nombre = dto.Nombre;
            categoria.Descripcion = dto.Descripcion;

            _context.Categoria.Update(categoria);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria == null) return false;

            _context.Categoria.Remove(categoria);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
