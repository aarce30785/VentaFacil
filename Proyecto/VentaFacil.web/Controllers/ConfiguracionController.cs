using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Configuracion;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class ConfiguracionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Limpieza proactiva de duplicados antes de cargar
            await LimpiarDuplicados();

            var deducciones = await _context.DeduccionLey.ToListAsync();
            var impuestos = await _context.ImpuestoRenta.OrderBy(i => i.LimiteInferior).ToListAsync();

            if (!deducciones.Any())
            {
                await InicializarValores();
                deducciones = await _context.DeduccionLey.ToListAsync();
            }

            if (!impuestos.Any())
            {
                 await InicializarValores();
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
            // Seed Deducciones de forma segura
            if (!await _context.DeduccionLey.AnyAsync(d => d.Nombre == "SEM"))
                _context.DeduccionLey.Add(new DeduccionLey { Nombre = "SEM", Porcentaje = 5.50m, Activo = true });

            if (!await _context.DeduccionLey.AnyAsync(d => d.Nombre == "IVM"))
                _context.DeduccionLey.Add(new DeduccionLey { Nombre = "IVM", Porcentaje = 4.17m, Activo = true });
            
            if (!await _context.DeduccionLey.AnyAsync(d => d.Nombre == "LPT"))
                _context.DeduccionLey.Add(new DeduccionLey { Nombre = "LPT", Porcentaje = 1.00m, Activo = true });

            // Seed Impuestos de forma segura (verificando el primer tramo)
            if (!await _context.ImpuestoRenta.AnyAsync(i => i.Anio == 2025))
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

        private async Task LimpiarDuplicados()
        {
            // 1. Limpiar Deducciones Duplicadas
            // Agrupar por nombre, ordenar por ID descendente (mantener el más reciente o el primero, aquí el último creado)
            // En realidad, para configuración base, deberíamos mantener el ID más bajo (el original)
            
            var deducciones = await _context.DeduccionLey.ToListAsync();
            var duplicadosDeducciones = deducciones
                .GroupBy(d => d.Nombre)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderBy(d => d.Id).Skip(1)) // Saltamos el primero (ID más bajo), borramos el resto
                .ToList();

            if (duplicadosDeducciones.Any())
            {
                _context.DeduccionLey.RemoveRange(duplicadosDeducciones);
            }

            // 2. Limpiar Impuestos Duplicados
            // Asumimos que la unicidad es Anio + LimiteInferior
            var impuestos = await _context.ImpuestoRenta.ToListAsync();
            var duplicadosImpuestos = impuestos
                .GroupBy(i => new { i.Anio, i.LimiteInferior })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderBy(i => i.Id).Skip(1))
                .ToList();

            if (duplicadosImpuestos.Any())
            {
                _context.ImpuestoRenta.RemoveRange(duplicadosImpuestos);
            }

            if (duplicadosDeducciones.Any() || duplicadosImpuestos.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
