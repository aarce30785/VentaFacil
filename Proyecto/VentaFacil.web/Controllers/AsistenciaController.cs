using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VentaFacil.web.Data;
using VentaFacil.web.Models;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    public class AsistenciaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AsistenciaController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdString, out int userId);
            return userId;
        }

        [HttpGet("Asistencia")]
        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();

            // Buscar la jornada activa de HOY (que no tiene fecha de salida)
            var jornadaActiva = await _context.Planilla
                .Where(p => p.Id_Usr == userId && p.FechaInicio.Date == DateTime.Today.Date && p.EstadoRegistro == "Incompleta" && p.FechaFinal == null)
                .OrderByDescending(p => p.FechaInicio)
                .FirstOrDefaultAsync();

            ViewBag.JornadaActiva = jornadaActiva;

            // Listar planillas generadas recientes
            var recibos = await _context.Planilla
                .Include(p => p.Nomina)
                .Where(p => p.Id_Usr == userId && p.Id_Nomina != null && p.Nomina.Estado != "Anulada")
                .OrderByDescending(p => p.Nomina.FechaGeneracion)
                .Take(5)
                .ToListAsync();

            ViewBag.Recibos = recibos;

            return View();
        }

        [HttpPost("Asistencia/MarcarEntrada")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarEntrada()
        {
            int userId = GetUserId();

            // Verificar si ya tiene una activa
            var jornadaActiva = await _context.Planilla
                .Where(p => p.Id_Usr == userId && p.FechaInicio.Date == DateTime.Today.Date && p.EstadoRegistro == "Incompleta" && p.FechaFinal == null)
                .FirstOrDefaultAsync();

            if (jornadaActiva != null)
            {
                TempData["Error"] = "Ya tiene una jornada abierta hoy sin finalizar.";
                return RedirectToAction("Index");
            }

            var nuevaPlanilla = new Planilla
            {
                Id_Usr = userId,
                FechaInicio = DateTime.Now,
                EstadoRegistro = "Incompleta",
                HorasTrabajadas = 0,
                HorasExtras = 0,
                Bonificaciones = 0,
                Deducciones = 0,
                SalarioBruto = 0,
                SalarioNeto = 0
            };

            _context.Planilla.Add(nuevaPlanilla);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Entrada registrada correctamente a las {DateTime.Now.ToString("HH:mm")}";
            return RedirectToAction("Index");
        }

        [HttpPost("Asistencia/MarcarPausaInicio")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarPausaInicio()
        {
            int userId = GetUserId();
            var jornada = await _context.Planilla
                .Where(p => p.Id_Usr == userId && p.FechaInicio.Date == DateTime.Today.Date && p.EstadoRegistro == "Incompleta" && p.FechaFinal == null)
                .FirstOrDefaultAsync();

            if (jornada == null)
            {
                TempData["Error"] = "No tiene jornadas activas hoy.";
                return RedirectToAction("Index");
            }

            if (jornada.HoraInicioPausa != null)
            {
                TempData["Error"] = "Ya existe un inicio de pausa registrado.";
                return RedirectToAction("Index");
            }

            jornada.HoraInicioPausa = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Inicio de pausa registrado.";
            return RedirectToAction("Index");
        }

        [HttpPost("Asistencia/MarcarPausaFin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarPausaFin()
        {
            int userId = GetUserId();
            var jornada = await _context.Planilla
                .Where(p => p.Id_Usr == userId && p.FechaInicio.Date == DateTime.Today.Date && p.EstadoRegistro == "Incompleta" && p.FechaFinal == null)
                .FirstOrDefaultAsync();

            if (jornada == null || jornada.HoraInicioPausa == null)
            {
                TempData["Error"] = "No puede finalizar una pausa sin iniciarla.";
                return RedirectToAction("Index");
            }

            jornada.HoraFinPausa = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Fin de pausa registrado.";
            return RedirectToAction("Index");
        }

        [HttpPost("Asistencia/MarcarSalida")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarSalida()
        {
            int userId = GetUserId();
            var jornada = await _context.Planilla
                .Where(p => p.Id_Usr == userId && p.FechaInicio.Date == DateTime.Today.Date && p.EstadoRegistro == "Incompleta" && p.FechaFinal == null)
                .FirstOrDefaultAsync();

            if (jornada == null)
            {
                TempData["Error"] = "No tiene jornadas activas para cerrar.";
                return RedirectToAction("Index");
            }

            jornada.FechaFinal = DateTime.Now;
            jornada.EstadoRegistro = "Completada";

            // Calcular horas (Lógica duplicada del PlanillaService, idealmente se debe parametrizar en el repo, pero lo mantenemos simple)
            TimeSpan duracion = jornada.FechaFinal.Value - jornada.FechaInicio;
            double horasPausa = 0;
            if (jornada.HoraInicioPausa.HasValue && jornada.HoraFinPausa.HasValue)
            {
                horasPausa = (jornada.HoraFinPausa.Value - jornada.HoraInicioPausa.Value).TotalHours;
            }

            double horasEfectivas = duracion.TotalHours - horasPausa;
            if (horasEfectivas < 0) horasEfectivas = 0;

            // Lógica de pago: 
            // Máximo 12 horas pagables: 8 normales, 4 extras (1.5x)
            double horasNormales = Math.Min(horasEfectivas, 8.0);
            double horasExtras = 0;
            
            if (horasEfectivas > 8.0)
            {
                horasExtras = Math.Min(horasEfectivas - 8.0, 4.0); // Máximo 4 extras (hasta llegar a 12 total)
            }

            jornada.HorasTrabajadas = (decimal)horasNormales;
            jornada.HorasExtras = (decimal)horasExtras;

            var configuracion = await _context.ConfiguracionPlanilla.FirstOrDefaultAsync(c => c.Id_Usr == userId);
            decimal tarifaPorHora = configuracion?.TarifaPorHora ?? 2500m;

            jornada.SalarioBruto = (jornada.HorasTrabajadas * tarifaPorHora) + (jornada.HorasExtras * tarifaPorHora * 1.5m);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Salida registrada. Horas computadas: {horasNormales.ToString("0.##")}h normales y {horasExtras.ToString("0.##")}h extras.";
            return RedirectToAction("Index");
        }
    }
}
