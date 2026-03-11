using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Services.PDF;
using VentaFacil.web.Services.Usuario;

namespace VentaFacil.web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;
        private readonly IUsuarioService _usuarioService;

        public ReportesController(ApplicationDbContext context, IPdfService pdfService, IUsuarioService usuarioService)
        {
            _context = context;
            _pdfService = pdfService;
            _usuarioService = usuarioService;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuario.ToListAsync();
            
            ViewBag.Cajeros = usuarios.Select(u => new SelectListItem 
            {
                Value = u.Id_Usr.ToString(),
                Text = u.Nombre
            }).ToList();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VentasDiarias(DateTime fecha)
        {
            var facturas = await _context.Factura
                .Include(f => f.Venta)
                .ThenInclude(v => v.Usuario)
                .Where(f => f.FechaEmision.Date == fecha.Date && (f.Estado == VentaFacil.web.Models.Enum.EstadoFactura.Activa || f.Estado == VentaFacil.web.Models.Enum.EstadoFactura.Pendiente))
                .ToListAsync();

            if (!facturas.Any())
            {
                TempData["Error"] = "No hay datos para la fecha seleccionada.";
                return RedirectToAction(nameof(Index));
            }

            // AQUI GENERAR EL PDF CON _pdfService (AÑADIREMOS EL METODO MAS ADELANTE)
            // Por ahora retornaremos un ok para la estructura
            byte[] pdf = _pdfService.GenerarReporteVentasDiariasPdf(facturas, fecha);
            
            return File(pdf, "application/pdf", $"VentasDiarias_{fecha:yyyyMMdd}.pdf");
        }

        [HttpPost]
        public async Task<IActionResult> ReportePersonalizado(DateTime fechaInicio, DateTime fechaFin, int? idCajero)
        {
            var query = _context.Factura
                .Include(f => f.Venta)
                .ThenInclude(v => v.Usuario)
                .Where(f => f.FechaEmision.Date >= fechaInicio.Date && f.FechaEmision.Date <= fechaFin.Date)
                .Where(f => f.Estado == VentaFacil.web.Models.Enum.EstadoFactura.Activa || f.Estado == VentaFacil.web.Models.Enum.EstadoFactura.Pendiente);

            if (idCajero.HasValue && idCajero.Value > 0)
            {
                query = query.Where(f => f.Venta.Id_Usuario == idCajero.Value);
            }

            var facturas = await query.ToListAsync();

            if (!facturas.Any())
            {
                TempData["Error"] = "No hay datos para el rango de fechas y cajero seleccionados.";
                return RedirectToAction(nameof(Index));
            }

            byte[] excel = _pdfService.GenerarReportePersonalizadoExcel(facturas, fechaInicio, fechaFin);
            
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ReportePersonalizado_{DateTime.Now:yyyyMMddHHmm}.xlsx");
        }
    }
}
