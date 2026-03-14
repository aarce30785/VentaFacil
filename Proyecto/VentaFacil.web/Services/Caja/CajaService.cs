using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using VentaFacil.web.Services.Email;

namespace VentaFacil.web.Services.Caja
{
    public class CajaService : ICajaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public CajaService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

        public async Task<bool> ExisteCajaAbiertaAsync(int? idUsuario = null)
        {
            var query = _context.Caja.Where(c => c.Estado == "Abierta");
            
            if (idUsuario.HasValue && idUsuario.Value > 0)
            {
                query = query.Where(c => c.Id_Usuario == idUsuario.Value);
            }

            return await query.AnyAsync();
        }

        public async Task CerrarCajasExcedidasAsync()
        {
            var hoy = DateTime.Today;
            var cajasAbiertas = await _context.Caja
                .Where(c => c.Estado == "Abierta" && c.Fecha_Apertura < hoy)
                .ToListAsync();

            if (!cajasAbiertas.Any()) return;

            // Necesitamos el servicio de email pero no podemos inyectarlo directamente en el constructor si queremos evitar ciclos o si se registra como Scoped
            // En este caso, CajaService es Scoped, EmailService es Scoped. 
            // Pero CerrarCajasExcedidasAsync será llamado desde un BackgroundService (Singleton) que crea un scope.
            
            foreach (var caja in cajasAbiertas)
            {
                try
                {
                    // Obtener usuario para el correo
                    var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.Id_Usr == caja.Id_Usuario);
                    
                    // Cerrar la caja usando el monto esperado como físico
                    await CerrarCajaAsync(caja.Id_Caja, caja.Id_Usuario, caja.Monto ?? 0, caja.Monto_USD ?? 0, "CIERRE AUTOMÁTICO - Olvido de cierre de caja por el usuario.");

                    if (usuario != null && !string.IsNullOrEmpty(usuario.Correo))
                    {
                        // Intentar enviar correo (usando IServiceProvider o inyectando IEmailService)
                        // Para simplificar esta parte, asumiremos que IEmailService está disponible vía constructor si lo agregamos.
                        // Sin embargo, para evitar romper el constructor actual, lo buscaré vía el contexto si fuera posible o lo inyectaré.
                        // ACTUALIZACIÓN: Inyectaré IEmailService en el constructor de CajaService.
                        
                        var subject = "Notificación de Cierre Automático de Caja";
                        var body = $@"
                            <h2>Aviso de Cierre de Caja</h2>
                            <p>Estimado/a {usuario.Nombre},</p>
                            <p>Le informamos que su caja abierta el día <b>{caja.Fecha_Apertura:dd/MM/yyyy HH:mm}</b> ha sido cerrada automáticamente por el sistema al detectar que no fue cerrada al finalizar su jornada.</p>
                            <p>El cierre se realizó utilizando los montos registrados en el sistema como saldos finales.</p>
                            <p>Por favor, revise su estado de caja en el módulo administrativo.</p>
                            <br/>
                            <p>Atentamente,<br/>Sistema VentaFácil</p>";
                        
                        await _emailService.SendEmailAsync(usuario.Correo, subject, body);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other cajas
                    Console.WriteLine($"Error cerrando caja automática {caja.Id_Caja}: {ex.Message}");
                }
            }
        }
    }
}
