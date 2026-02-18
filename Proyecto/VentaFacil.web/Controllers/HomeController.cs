using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VentaFacil.web.Models;
using VentaFacil.web.Models.ViewModel;
using VentaFacil.web.Services.Pedido;
using VentaFacil.web.Services.Producto;
using VentaFacil.web.Services.Inventario;
using VentaFacil.web.Services.Facturacion;
using VentaFacil.web.Services.Caja;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPedidoService _pedidoService;
        private readonly IProductoService _productoService;
        private readonly IInventarioService _inventarioService;
        private readonly IFacturacionService _facturacionService;
        private readonly ICajaService _cajaService;

        public HomeController(
            ILogger<HomeController> logger,
            IPedidoService pedidoService,
            IProductoService productoService,
            IInventarioService inventarioService,
            IFacturacionService facturacionService,
            ICajaService cajaService)
        {
            _logger = logger;
            _pedidoService = pedidoService;
            _productoService = productoService;
            _inventarioService = inventarioService;
            _facturacionService = facturacionService;
            _cajaService = cajaService;
        }

        public async Task<IActionResult> Index()
        {
            // TODO: Remove this test alert after verification
            // Test alert removed

            var dashboard = new DashboardViewModel();

            dashboard.VentasDia = await _facturacionService.GetVentasDiaAsync();
            dashboard.VentasSemana = await _facturacionService.GetVentasSemanaAsync();
            dashboard.VentasMes = await _facturacionService.GetVentasMesAsync();

            dashboard.ProductosMasVendidos = await _productoService.GetProductosMasVendidosAsync();
            dashboard.StockMinimo = await _inventarioService.GetStockMinimoAsync();
            dashboard.IngresosRecientes = await _cajaService.GetIngresosRecientesAsync();
            dashboard.GastosRecientes = await _cajaService.GetGastosRecientesAsync();

            return View(dashboard);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
