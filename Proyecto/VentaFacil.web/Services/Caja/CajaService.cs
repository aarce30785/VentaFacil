using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace VentaFacil.web.Services.Caja
{
    public class CajaService : ICajaService
    {
        private readonly ApplicationDbContext _context;

        public CajaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<VentaFacil.web.Models.Caja> AbrirCajaAsync(int idUsuario, decimal montoInicial)
        {
            var nuevaCaja = new VentaFacil.web.Models.Caja
            {
                Id_Usuario = idUsuario,
                Fecha_Apertura = DateTime.Now,
                Monto_Inicial = montoInicial,
                Monto = montoInicial,
                Estado = "Abierta"
            };

            _context.Caja.Add(nuevaCaja);
            await _context.SaveChangesAsync();
            return nuevaCaja;
        }

        public async Task<VentaFacil.web.Models.Caja> CerrarCajaAsync(int idCaja)
        {
            var caja = await _context.Caja.FirstOrDefaultAsync(c => c.Id_Caja == idCaja && c.Estado == "Abierta");
            if (caja == null)
                throw new InvalidOperationException("Caja no encontrada o ya cerrada.");

            caja.Fecha_Cierre = DateTime.Now;
            caja.Estado = "Cerrada";

            await _context.SaveChangesAsync();
            return caja;
        }

        public async Task<CajaRetiro> RegistrarRetiroAsync(int idCaja, int idUsuario, decimal monto, string motivo)
        {
            var caja = await _context.Caja.FirstOrDefaultAsync(c => c.Id_Caja == idCaja && c.Estado == "Abierta");
            if (caja == null)
                throw new InvalidOperationException("Caja no encontrada o ya cerrada.");

            caja.Monto = caja.Monto -= monto;

            var retiro = new CajaRetiro
            {
                Id_Caja = idCaja,
                Id_Usuario = idUsuario,
                Monto = monto,
                Motivo = motivo,
                FechaHora = DateTime.Now
            };

            _context.CajaRetiro.Add(retiro);
            await _context.SaveChangesAsync();
            return retiro;
        }

        public async Task<List<VentaFacil.web.Models.Caja>> ListarCajasAsync()
        {
            return await _context.Caja.ToListAsync();
        }

        public async Task<List<CajaRetiro>> ObtenerRetirosPorCajaAsync(int idCaja)
        {
            return await _context.CajaRetiro
                .Where(r => r.Id_Caja == idCaja)
                .ToListAsync();
        }

        public async Task<decimal> GetIngresosRecientesAsync()
        {
            var desde = DateTime.Today.AddDays(-7);
            return await _context.CajaRetiro
                .Where(r => r.FechaHora >= desde && r.Monto > 0)
                .SumAsync(r => r.Monto);
        }

        public async Task<decimal> GetGastosRecientesAsync()
        {
            var desde = DateTime.Today.AddDays(-7);
            return await _context.CajaRetiro
                .Where(r => r.FechaHora >= desde && r.Monto < 0)
                .SumAsync(r => r.Monto);
        }
    }
}
