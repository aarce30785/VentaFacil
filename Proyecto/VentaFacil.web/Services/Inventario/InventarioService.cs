using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using System;

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
                    StockMinimo = inv.StockMinimo
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
                StockMinimo = inv.StockMinimo
            };
        }

        // Registrar nuevo inventario
        public async Task<bool> RegistrarAsync(InventarioDto dto)
        {
            var inventario = new Models.Inventario
            {
                Nombre = dto.Nombre,
                StockActual = dto.StockActual,
                StockMinimo = dto.StockMinimo
            };
            _context.Inventario.Add(inventario);
            await _context.SaveChangesAsync();
            return true;
        }

        // Editar inventario existente
        public async Task<bool> EditarAsync(InventarioDto dto)
        {
            var inventario = await _context.Inventario.FindAsync(dto.Id_Inventario);
            if (inventario == null) return false;

            int diferencia = dto.StockActual - inventario.StockActual;
            inventario.Nombre = dto.Nombre;
            inventario.StockActual = dto.StockActual;
            inventario.StockMinimo = dto.StockMinimo;
            await _context.SaveChangesAsync();

            if (diferencia != 0)
            {
                var movimiento = new InventarioMovimiento
                {
                    Id_Inventario = dto.Id_Inventario,
                    Tipo_Movimiento = diferencia > 0 ? "Ajuste Entrada" : "Ajuste Salida",
                    Cantidad = Math.Abs(diferencia),
                    Fecha = DateTime.Now,
                    Id_Usuario = 1 // Asignar el ID del usuario correspondiente
                };
                _context.InventarioMovimiento.Add(movimiento);
                await _context.SaveChangesAsync();
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

        public async Task<bool> AgregarUnidadAsync(int id)
        {
            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null) return false;

            inventario.StockActual += 1;
            await _context.SaveChangesAsync();

            // Registrar movimiento
            var movimiento = new InventarioMovimiento
            {
                Id_Inventario = id,
                Tipo_Movimiento = "Entrada",
                Cantidad = 1,
                Fecha = DateTime.Now,
                Id_Usuario = 1 // Asignar el ID del usuario correspondiente
            };
            _context.InventarioMovimiento.Add(movimiento);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> QuitarUnidadAsync(int id)
        {
            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null) return false;
            if (inventario.StockActual > 0)
            {
                inventario.StockActual -= 1;
                await _context.SaveChangesAsync();

                // Registrar movimiento
                var movimiento = new InventarioMovimiento
                {
                    Id_Inventario = id,
                    Tipo_Movimiento = "Salida",
                    Cantidad = 1,
                    Fecha = DateTime.Now,
                    Id_Usuario = 1 // Asignar el ID del usuario correspondiente 
                };
                _context.InventarioMovimiento.Add(movimiento);
                await _context.SaveChangesAsync();

                return true;
            }
            return false;
        }
    }
}
