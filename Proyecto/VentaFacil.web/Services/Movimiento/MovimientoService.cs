using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Services.Movimiento
{
    public class MovimientoService : IMovimientoService
    {
        private readonly ApplicationDbContext _context;

        public MovimientoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventarioMovimientoDto>> ListarMovimientosAsync(int? idInventario, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.InventarioMovimiento.AsQueryable();

            if (idInventario.HasValue)
                query = query.Where(m => m.Id_Inventario == idInventario.Value);

            if (fechaInicio.HasValue)
                query = query.Where(m => m.Fecha >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(m => m.Fecha <= fechaFin.Value);

            return await query
                .OrderByDescending(m => m.Fecha)
                .Select(m => new InventarioMovimientoDto
                {
                    Id_Movimiento = m.Id_Movimiento,
                    Id_Inventario = m.Id_Inventario,
                    Tipo_Movimiento = m.Tipo_Movimiento,
                    Cantidad = m.Cantidad,
                    Fecha = m.Fecha,
                    Id_Usuario = m.Id_Usuario
                })
                .ToListAsync();
        }
    }
}
