using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VentaFacil.web.Services.Caja
{
    public class CajaAutoCloseService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CajaAutoCloseService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public CajaAutoCloseService(IServiceProvider serviceProvider, ILogger<CajaAutoCloseService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CajaAutoCloseService iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    // Ejecutar el cierre si es pasada la medianoche (entre 00:00 y 01:00 o simplemente en cada intervalo de hora)
                    // Para ser más robustos, lo ejecutamos siempre que el servicio corra, 
                    // ya que el método CerrarCajasExcedidasAsync filtra por Fecha_Apertura < Hoy.
                    
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var cajaService = scope.ServiceProvider.GetRequiredService<ICajaService>();
                        _logger.LogInformation("Verificando cajas abiertas de días anteriores...");
                        await cajaService.CerrarCajasExcedidasAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en CajaAutoCloseService.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("CajaAutoCloseService detenido.");
        }
    }
}
