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
                    Id_Usuario = m.Id_Usuario,
                    Nombre_Usuario = _context.Usuario.Where(u => u.Id_Usr == m.Id_Usuario).Select(u => u.Nombre).FirstOrDefault() ?? "Sistema"
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

        public async Task<bool> AnularMovimientoAsync(int idMovimiento, string motivo, int idUsuario)
        {
            var movimiento = await _context.InventarioMovimiento.FindAsync(idMovimiento);
            if (movimiento == null || movimiento.Tipo_Movimiento == "Anulado") return false;

            var inventario = await _context.Inventario.FindAsync(movimiento.Id_Inventario);
            if (inventario == null) return false;

            // Si fue una Entrada/Inicial/A.Entrada → al anular se RESTA stock (reversa = Salida)
            // Si fue una Salida/A.Salida → al anular se SUMA stock (reversa = Entrada)
            bool esEntrada = movimiento.Tipo_Movimiento == "Entrada" ||
                             movimiento.Tipo_Movimiento == "Inventario Inicial" ||
                             movimiento.Tipo_Movimiento == "Ajuste Entrada";

            int stockAnterior = inventario.StockActual;

            if (esEntrada)
                inventario.StockActual -= movimiento.Cantidad;
            else
                inventario.StockActual += movimiento.Cantidad;

            if (inventario.StockActual < 0) return false;

            // 1. Registrar auditoría interna
            var auditoria = new InventarioMovimientoAuditoria
            {
                Id_Movimiento   = movimiento.Id_Movimiento,
                Id_Inventario   = movimiento.Id_Inventario,
                CantidadAnterior       = stockAnterior,
                CantidadNueva          = inventario.StockActual,
                TipoMovimientoAnterior = movimiento.Tipo_Movimiento,
                TipoMovimientoNuevo    = "Anulado",
                MotivoCambio           = motivo,
                Id_UsuarioResponsable  = idUsuario,
                FechaCambio            = DateTime.Now
            };
            _context.InventarioMovimientoAuditoria.Add(auditoria);

            // 2. Marcar el movimiento original como Anulado
            movimiento.Tipo_Movimiento = "Anulado";

            // 3. Crear un nuevo movimiento de reversa VISIBLE en el kardex
            var tipoReversa = esEntrada ? "Reversa por Anulación (Salida)" : "Reversa por Anulación (Entrada)";
            var movimientoReversa = new InventarioMovimiento
            {
                Id_Inventario    = movimiento.Id_Inventario,
                Tipo_Movimiento  = tipoReversa,
                Cantidad         = movimiento.Cantidad,
                Fecha            = DateTime.Now,
                Id_Usuario       = idUsuario,
                Observaciones    = $"Anulación del movimiento #{movimiento.Id_Movimiento}. Motivo: {motivo}"
            };
            _context.InventarioMovimiento.Add(movimientoReversa);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
