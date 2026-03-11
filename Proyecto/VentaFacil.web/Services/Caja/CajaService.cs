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

        public async Task<VentaFacil.web.Models.Caja> AbrirCajaAsync(int idUsuario, decimal montoInicial, decimal montoInicialUSD)
        {
            var nuevaCaja = new VentaFacil.web.Models.Caja
            {
                Id_Usuario = idUsuario,
                Fecha_Apertura = DateTime.Now,
                Monto_Inicial = montoInicial,
                Monto_Inicial_USD = montoInicialUSD,
                Monto = montoInicial,
                Monto_USD = montoInicialUSD,
                Estado = "Abierta"
            };

            _context.Caja.Add(nuevaCaja);
            await _context.SaveChangesAsync();
            return nuevaCaja;
        }

        public async Task<VentaFacil.web.Models.Caja> CerrarCajaAsync(int idCaja, int idUsuario, decimal montoFisico, decimal montoFisicoUSD, string justificacion)
        {
            var caja = await _context.Caja.FirstOrDefaultAsync(c => c.Id_Caja == idCaja && c.Estado == "Abierta");
            if (caja == null)
                throw new InvalidOperationException("Caja no encontrada o ya cerrada.");

            var difCRC = montoFisico - (caja.Monto ?? 0);
            var difUSD = montoFisicoUSD - (caja.Monto_USD ?? 0);
            
            if (Math.Abs(difCRC) > 0.01m || Math.Abs(difUSD) > 0.01m)
            {
                if (string.IsNullOrWhiteSpace(justificacion))
                {
                    throw new InvalidOperationException("Se requiere una justificación debido a la discrepancia entre el monto esperado y el monto físico.");
                }

                // Ajuste CRC
                if (Math.Abs(difCRC) > 0.01m)
                {
                    var ajusteCRC = new CajaRetiro
                    {
                        Id_Caja = idCaja,
                        Id_Usuario = idUsuario,
                        Monto = difCRC,
                        Moneda = "CRC",
                        Motivo = $"Ajuste en Cierre COLONES ({(difCRC > 0 ? "Sobrante" : "Faltante")}): {justificacion}",
                        FechaHora = DateTime.Now
                    };
                    _context.CajaRetiro.Add(ajusteCRC);
                    caja.Monto = (caja.Monto ?? 0) + difCRC;
                }

                // Ajuste USD
                if (Math.Abs(difUSD) > 0.01m)
                {
                    var ajusteUSD = new CajaRetiro
                    {
                        Id_Caja = idCaja,
                        Id_Usuario = idUsuario,
                        Monto = difUSD,
                        Moneda = "USD",
                        Motivo = $"Ajuste en Cierre DÓLARES ({(difUSD > 0 ? "Sobrante" : "Faltante")}): {justificacion}",
                        FechaHora = DateTime.Now
                    };
                    _context.CajaRetiro.Add(ajusteUSD);
                    caja.Monto_USD = (caja.Monto_USD ?? 0) + difUSD;
                }
            }

            caja.Fecha_Cierre = DateTime.Now;
            caja.Estado = "Cerrada";

            await _context.SaveChangesAsync();
            return caja;
        }

        public async Task<CajaRetiro> RegistrarIngresoAsync(int idCaja, int idUsuario, decimal monto, string motivo, string moneda = "CRC")
        {
            var caja = await _context.Caja.FirstOrDefaultAsync(c => c.Id_Caja == idCaja && c.Estado == "Abierta");
            if (caja == null)
                throw new InvalidOperationException("Caja no encontrada o ya cerrada.");

            if (moneda == "USD") caja.Monto_USD += monto;
            else caja.Monto += monto;

            var ingreso = new CajaRetiro
            {
                Id_Caja = idCaja,
                Id_Usuario = idUsuario,
                Monto = monto,
                Moneda = moneda,
                Motivo = motivo,
                FechaHora = DateTime.Now
            };

            _context.CajaRetiro.Add(ingreso);
            await _context.SaveChangesAsync();
            return ingreso;
        }

        public async Task<CajaRetiro> RegistrarRetiroAsync(int idCaja, int idUsuario, decimal monto, string motivo, string moneda = "CRC")
        {
            var caja = await _context.Caja.FirstOrDefaultAsync(c => c.Id_Caja == idCaja && c.Estado == "Abierta");
            if (caja == null)
                throw new InvalidOperationException("Caja no encontrada o ya cerrada.");

            if (moneda == "USD") caja.Monto_USD -= monto;
            else caja.Monto -= monto;

            var retiro = new CajaRetiro
            {
                Id_Caja = idCaja,
                Id_Usuario = idUsuario,
                Monto = -monto, // Guardar como negativo para reflejar gasto
                Moneda = moneda,
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
