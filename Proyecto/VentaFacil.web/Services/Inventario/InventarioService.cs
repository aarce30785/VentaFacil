using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using System;
using System.Linq;

namespace VentaFacil.web.Services.Inventario
{
    public class InventarioService : IInventarioService
    {
        private readonly ApplicationDbContext _context;

        public InventarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Listar todos los inventarios
        public async Task<List<InventarioDto>> ListarTodosAsync()
        {
            var inventarios = await _context.Inventario.ToListAsync();
            var dtos = new List<InventarioDto>();
            foreach (var inv in inventarios)
            {
                dtos.Add(new InventarioDto
                {
                    Id_Inventario = inv.Id_Inventario,
                    Nombre = inv.Nombre,
                    StockActual = inv.StockActual,
                    StockMinimo = inv.StockMinimo,
                    UnidadMedida = inv.UnidadMedida
                });
            }
            return dtos;
        }

        // Obtener inventario por ID
        public async Task<InventarioDto?> GetByIdAsync(int id)
        {
            var inv = await _context.Inventario.FindAsync(id);
            if (inv == null) return null;
            return new InventarioDto
            {
                Id_Inventario = inv.Id_Inventario,
                Nombre = inv.Nombre,
                StockActual = inv.StockActual,
                StockMinimo = inv.StockMinimo,
                UnidadMedida = inv.UnidadMedida
            };
        }

        // Registrar nuevo inventario
        public async Task<bool> RegistrarAsync(InventarioDto dto)
        {
            var inventario = new Models.Inventario
            {
                Nombre = dto.Nombre,
                StockActual = dto.StockActual,
                StockMinimo = dto.StockMinimo,
                UnidadMedida = dto.UnidadMedida
            };
            _context.Inventario.Add(inventario);
            await _context.SaveChangesAsync();

            // Si hay stock inicial, registrar movimiento y auditoría
            if (dto.StockActual > 0)
            {
                var usuarioId = 1; 
                // Usaremos 1 como un fallback o "Sistema".

                var movimiento = new InventarioMovimiento
                {
                    Id_Inventario = inventario.Id_Inventario,
                    Tipo_Movimiento = "Inventario Inicial",
                    Cantidad = dto.StockActual,
                    Fecha = DateTime.Now,
                    Id_Usuario = usuarioId
                };
                _context.InventarioMovimiento.Add(movimiento);
                await _context.SaveChangesAsync();

                await RegistrarAuditoriaAsync(movimiento.Id_Movimiento, inventario.Id_Inventario, 0, dto.StockActual, "Inventario Inicial", "Creación de Inventario", usuarioId);
            }

            return true;
        }

        // Editar inventario existente
        public async Task<bool> EditarAsync(InventarioDto dto, int idUsuario)
        {
            var inventario = await _context.Inventario.FindAsync(dto.Id_Inventario);
            if (inventario == null) return false;

            int diferencia = dto.StockActual - inventario.StockActual;
            inventario.Nombre = dto.Nombre;
            inventario.StockActual = dto.StockActual;
            inventario.StockMinimo = dto.StockMinimo;
            inventario.UnidadMedida = dto.UnidadMedida;
            await _context.SaveChangesAsync();

            if (diferencia != 0)
            {
                var stockAnterior = inventario.StockActual - diferencia;
                var movimiento = new InventarioMovimiento
                {
                    Id_Inventario = dto.Id_Inventario,
                    Tipo_Movimiento = diferencia > 0 ? "Ajuste Entrada" : "Ajuste Salida",
                    Cantidad = Math.Abs(diferencia),
                    Fecha = DateTime.Now,
                    Id_Usuario = idUsuario 
                };
                _context.InventarioMovimiento.Add(movimiento);
                await _context.SaveChangesAsync();

                await RegistrarAuditoriaAsync(movimiento.Id_Movimiento, movimiento.Id_Inventario, stockAnterior, inventario.StockActual, movimiento.Tipo_Movimiento, "Ajuste de Inventario", idUsuario);
            }

            return true;
        }

        // Eliminar inventario
        public async Task<bool> EliminarAsync(int id)
        {
            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null) return false;
            _context.Inventario.Remove(inventario);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AgregarUnidadAsync(int id, int idUsuario)
        {
            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null) return false;

            var stockAnterior = inventario.StockActual;
            inventario.StockActual += 1;
            await _context.SaveChangesAsync();

            // Registrar movimiento
            var movimiento = new InventarioMovimiento
            {
                Id_Inventario = id,
                Tipo_Movimiento = "Entrada",
                Cantidad = 1,
                Fecha = DateTime.Now,
                Id_Usuario = idUsuario // Asignar el ID del usuario correspondiente
            };
            _context.InventarioMovimiento.Add(movimiento);
            await _context.SaveChangesAsync();

            await RegistrarAuditoriaAsync(movimiento.Id_Movimiento, movimiento.Id_Inventario, stockAnterior, inventario.StockActual, movimiento.Tipo_Movimiento, "Agregar Unidad Rápida", idUsuario);

            return true;
        }

        public async Task<bool> QuitarUnidadAsync(int id, int idUsuario)
        {
            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null) return false;
            if (inventario.StockActual > 0)
            {
                var stockAnterior = inventario.StockActual;
                inventario.StockActual -= 1;
                await _context.SaveChangesAsync();

                // Registrar movimiento
                var movimiento = new InventarioMovimiento
                {
                    Id_Inventario = id,
                    Tipo_Movimiento = "Salida",
                    Cantidad = 1,
                    Fecha = DateTime.Now,
                    Id_Usuario = idUsuario // Asignar el ID del usuario correspondiente 
                };
                _context.InventarioMovimiento.Add(movimiento);
                await _context.SaveChangesAsync();

                await RegistrarAuditoriaAsync(movimiento.Id_Movimiento, movimiento.Id_Inventario, stockAnterior, inventario.StockActual, movimiento.Tipo_Movimiento, "Quitar Unidad Rápida", idUsuario);

                return true;
            }
            return false;
        }

        public async Task<bool> RegistrarEntradaAsync(int idInventario, int cantidad, string observaciones, int idUsuario)
        {
            var inventario = await _context.Inventario.FindAsync(idInventario);
            if (inventario == null) return false;

            var stockAnterior = inventario.StockActual;
            inventario.StockActual += cantidad;
            await _context.SaveChangesAsync();

            var movimiento = new InventarioMovimiento
            {
                Id_Inventario = idInventario,
                Tipo_Movimiento = "Entrada",
                Cantidad = cantidad,
                Fecha = DateTime.Now,
                Id_Usuario = idUsuario,
                Observaciones = observaciones
            };
            _context.InventarioMovimiento.Add(movimiento);
            await _context.SaveChangesAsync();

            await RegistrarAuditoriaAsync(movimiento.Id_Movimiento, movimiento.Id_Inventario, stockAnterior, inventario.StockActual, movimiento.Tipo_Movimiento, observaciones ?? "Registro de Entrada", idUsuario);

            return true;
        }
        private async Task RegistrarAuditoriaAsync(int idMovimiento, int idInventario, int cantidadAnterior, int cantidadNueva, string tipoNuevo, string motivo, int idUsuario)
        {
            var auditoria = new InventarioMovimientoAuditoria
            {
                Id_Movimiento = idMovimiento,
                Id_Inventario = idInventario,
                CantidadAnterior = cantidadAnterior,
                CantidadNueva = cantidadNueva,
                TipoMovimientoAnterior = "Stock Anterior",
                TipoMovimientoNuevo = tipoNuevo,
                MotivoCambio = motivo,
                Id_UsuarioResponsable = idUsuario,
                FechaCambio = DateTime.Now
            };
            _context.InventarioMovimientoAuditoria.Add(auditoria);
            await _context.SaveChangesAsync();
        }

        public async Task<List<InventarioDto>> ObtenerStockMinimoAsync()
        {
            return await _context.Inventario
                .Where(i => i.StockActual <= i.StockMinimo)
                .Select(i => new InventarioDto
                {
                    Id_Inventario = i.Id_Inventario,
                    Nombre = i.Nombre,
                    StockActual = i.StockActual,
                    StockMinimo = i.StockMinimo
                })
                .ToListAsync();
        }
    }
}
