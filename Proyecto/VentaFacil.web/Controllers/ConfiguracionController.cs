using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Configuracion;

namespace VentaFacil.web.Controllers
{
    public class ConfiguracionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var deducciones = await _context.DeduccionLey.ToListAsync();
            var impuestos = await _context.ImpuestoRenta.OrderBy(i => i.LimiteInferior).ToListAsync();

            if (!deducciones.Any())
            {
                // Inicializar si está vacío (Seed básico)
                await InicializarValores();
                deducciones = await _context.DeduccionLey.ToListAsync();
                impuestos = await _context.ImpuestoRenta.OrderBy(i => i.LimiteInferior).ToListAsync();
            }

            ViewBag.Deducciones = deducciones;
            ViewBag.Impuestos = impuestos;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarDeduccion(int id, decimal porcentaje, bool activo)
        {
            var deduccion = await _context.DeduccionLey.FindAsync(id);
            if (deduccion != null)
            {
                deduccion.Porcentaje = porcentaje;
                deduccion.Activo = activo;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Deducción {deduccion.Nombre} actualizada.";
            }
            else
            {
                TempData["Error"] = "Deducción no encontrada.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarImpuesto(int id, decimal limiteInferior, decimal? limiteSuperior, decimal porcentaje)
        {
            var impuesto = await _context.ImpuestoRenta.FindAsync(id);
            if (impuesto != null)
            {
                impuesto.LimiteInferior = limiteInferior;
                impuesto.LimiteSuperior = limiteSuperior;
                impuesto.Porcentaje = porcentaje;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tramo de impuesto actualizado.";
            }
            else
            {
                // Crear nuevo si ID es 0 (opcional, por ahora solo editamos existentes)
                if (id == 0)
                {
                    var nuevoImpuesto = new ImpuestoRenta
                    {
                        Anio = 2025,
                        LimiteInferior = limiteInferior,
                        LimiteSuperior = limiteSuperior,
                        Porcentaje = porcentaje
                    };
                    _context.ImpuestoRenta.Add(nuevoImpuesto);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Nuevo tramo agregado.";
                }
                else
                {
                    TempData["Error"] = "Tramo no encontrado.";
                }
            }
            return RedirectToAction("Index");
        }

        private async Task InicializarValores()
        {
            if (!_context.DeduccionLey.Any())
            {
                _context.DeduccionLey.AddRange(
                    new DeduccionLey { Nombre = "SEM", Porcentaje = 5.50m, Activo = true },
                    new DeduccionLey { Nombre = "IVM", Porcentaje = 4.17m, Activo = true },
                    new DeduccionLey { Nombre = "LPT", Porcentaje = 1.00m, Activo = true }
                );
            }

            if (!_context.ImpuestoRenta.Any())
            {
                _context.ImpuestoRenta.AddRange(
                    new ImpuestoRenta { Anio = 2025, LimiteInferior = 0, LimiteSuperior = 922000, Porcentaje = 0 },
                    new ImpuestoRenta { Anio = 2025, LimiteInferior = 922000, LimiteSuperior = 1363000, Porcentaje = 10 },
                    new ImpuestoRenta { Anio = 2025, LimiteInferior = 1363000, LimiteSuperior = 2374000, Porcentaje = 15 },
                    new ImpuestoRenta { Anio = 2025, LimiteInferior = 2374000, LimiteSuperior = 4745000, Porcentaje = 20 },
                    new ImpuestoRenta { Anio = 2025, LimiteInferior = 4745000, LimiteSuperior = null, Porcentaje = 25 }
                );
            }

            await _context.SaveChangesAsync();
        }
    }
}
