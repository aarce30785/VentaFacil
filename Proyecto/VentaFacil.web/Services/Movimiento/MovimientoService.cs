using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models;

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

        public async Task<bool> CorregirMovimientoAsync(int idMovimiento, int nuevaCantidad, string nuevoTipo, string motivo, int idUsuario)
        {
            var movimiento = await _context.InventarioMovimiento.FindAsync(idMovimiento);
            if (movimiento == null) return false;

            var inventario = await _context.Inventario.FindAsync(movimiento.Id_Inventario);
            if (inventario == null) return false;

            
            bool esEntradaAnterior = movimiento.Tipo_Movimiento == "Entrada" || 
                                     movimiento.Tipo_Movimiento == "Inventario Inicial" || 
                                     movimiento.Tipo_Movimiento == "Ajuste Entrada";
            
            int efectoAnterior = esEntradaAnterior ? movimiento.Cantidad : -movimiento.Cantidad;

            
            bool esEntradaNueva = nuevoTipo == "Entrada" || 
                                  nuevoTipo == "Inventario Inicial" || 
                                  nuevoTipo == "Ajuste Entrada";

            int nuevoEfecto = esEntradaNueva ? nuevaCantidad : -nuevaCantidad;

            
            int stockAnterior = inventario.StockActual;

           
            inventario.StockActual = inventario.StockActual - efectoAnterior + nuevoEfecto;

            if (inventario.StockActual < 0) return false; 

            
            var auditoria = new InventarioMovimientoAuditoria
            {
                Id_Movimiento = movimiento.Id_Movimiento,
                Id_Inventario = movimiento.Id_Inventario,
                CantidadAnterior = stockAnterior,
                CantidadNueva = inventario.StockActual,
                TipoMovimientoAnterior = movimiento.Tipo_Movimiento,
                TipoMovimientoNuevo = nuevoTipo,
                MotivoCambio = motivo,
                Id_UsuarioResponsable = idUsuario,
                FechaCambio = DateTime.Now
            };
            _context.InventarioMovimientoAuditoria.Add(auditoria);

            // Actualizar movimiento
            movimiento.Cantidad = nuevaCantidad;
            movimiento.Tipo_Movimiento = nuevoTipo;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
