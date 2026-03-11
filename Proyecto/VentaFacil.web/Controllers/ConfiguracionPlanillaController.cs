using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using VentaFacil.web.Data;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Configuracion;

namespace VentaFacil.web.Controllers
{
    [Authorize(Roles = "Administrador")]
    [Route("Planilla/Configuracion")]
    public class ConfiguracionPlanillaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracionPlanillaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Obtener todos los usuarios activos
            var usuarios = await _context.Usuario
                .Where(u => u.Estado)
                .OrderBy(u => u.Nombre)
                .ToListAsync();

            // Obtener sus configuraciones de planilla (si tienen)
            var idsUsuarios = usuarios.Select(u => u.Id_Usr).ToList();
            var configuraciones = await _context.ConfiguracionPlanilla
                .Where(c => idsUsuarios.Contains(c.Id_Usr))
                .ToDictionaryAsync(c => c.Id_Usr);

            // Obtener las deducciones de ley activas e inactivas
            var deduccionesLey = await _context.DeduccionLey
                .OrderBy(d => d.Nombre)
                .ToListAsync();

            ViewBag.Configuraciones = configuraciones;
            ViewBag.DeduccionesLey  = deduccionesLey;

            return View("~/Views/Planilla/Configuracion/Index.cshtml", usuarios);
        }

        // ─── Tarifa por Hora ────────────────────────────────────────────────────

        [HttpPost("GuardarTarifa")]
        public async Task<IActionResult> GuardarTarifa(int idUsr, decimal tarifaPorHora)
        {
            try
            {
                var usuario = await _context.Usuario.FindAsync(idUsr);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (tarifaPorHora <= 0)
                {
                    TempData["Error"] = "La tarifa por hora debe ser mayor a cero.";
                    return RedirectToAction(nameof(Index));
                }

                var config = await _context.ConfiguracionPlanilla.FirstOrDefaultAsync(c => c.Id_Usr == idUsr);

                if (config == null)
                {
                    config = new ConfiguracionPlanilla
                    {
                        Id_Usr = idUsr,
                        TarifaPorHora = tarifaPorHora,
                        FechaActualizacion = DateTime.Now
                    };
                    _context.ConfiguracionPlanilla.Add(config);
                }
                else
                {
                    config.TarifaPorHora = tarifaPorHora;
                    config.FechaActualizacion = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Salario base de {usuario.Nombre} actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar la tarifa: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ─── Tasas de Deducción ─────────────────────────────────────────────────

        /// <summary>Actualiza el porcentaje de una DeduccionLey.</summary>
        [HttpPost("GuardarDeduccion")]
        public async Task<IActionResult> GuardarDeduccion(int idDeduccion, decimal porcentaje)
        {
            try
            {
                var ded = await _context.DeduccionLey.FindAsync(idDeduccion);
                if (ded == null)
                {
                    TempData["Error"] = "Deducción no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                if (porcentaje < 0 || porcentaje > 100)
                {
                    TempData["Error"] = "El porcentaje debe estar entre 0 y 100.";
                    return RedirectToAction(nameof(Index));
                }

                ded.Porcentaje = porcentaje;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Tasa '{ded.Nombre}' actualizada a {porcentaje:0.##}%.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar la tasa: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>Activa o desactiva una DeduccionLey.</summary>
        [HttpPost("ToggleDeduccion")]
        public async Task<IActionResult> ToggleDeduccion(int idDeduccion)
        {
            try
            {
                var ded = await _context.DeduccionLey.FindAsync(idDeduccion);
                if (ded == null)
                {
                    TempData["Error"] = "Deducción no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                ded.Activo = !ded.Activo;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Deducción '{ded.Nombre}' {(ded.Activo ? "activada" : "desactivada")} correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cambiar el estado: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
