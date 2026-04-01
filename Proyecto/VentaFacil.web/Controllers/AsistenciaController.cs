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

            // Listar nóminas generadas recientes (agrupadas por nómina)
            var planillasRecibos = await _context.Planilla
                .Include(p => p.Nomina)
                .Where(p => p.Id_Usr == userId && p.Id_Nomina != null && p.Nomina.Estado != "Anulada")
                .ToListAsync();

            var recibosSemanales = planillasRecibos
                .GroupBy(p => p.Nomina)
                .Select(g => new VentaFacil.web.Models.Dto.NominaDetalleDto
                {
                    Id_Nomina = g.Key.Id_Nomina,
                    FechaInicio = g.Key.FechaInicio,
                    FechaFinal = g.Key.FechaFinal,
                    FechaGeneracion = g.Key.FechaGeneracion,
                    TotalBruto = g.Sum(p => p.SalarioBruto),
                    TotalDeducciones = g.Sum(p => p.Deducciones),
                    TotalNeto = g.Sum(p => p.SalarioNeto)
                })
                .OrderByDescending(r => r.FechaGeneracion)
                .Take(5)
                .ToList();

            ViewBag.Recibos = recibosSemanales;

            return View();
        }

        [HttpPost("Asistencia/MarcarEntrada")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarEntrada()
        {
            int userId = GetUserId();

            // Verificar si el usuario tiene tarifa por hora asignada
            var configuracion = await _context.ConfiguracionPlanilla.FirstOrDefaultAsync(c => c.Id_Usr == userId);
            if (configuracion == null || configuracion.TarifaPorHora <= 0)
            {
                if (User.IsInRole("Administrador"))
                {
                    TempData["Error"] = "No tienes una tarifa por hora asignada. Redirigido a la configuración de planilla.";
                    return RedirectToAction("Index", "ConfiguracionPlanilla");
                }
                else
                {
                    TempData["Error"] = "No se puede iniciar jornada: tarifa por hora no asignada. Por favor contacte a un administrador.";
                    return RedirectToAction("Index");
                }
            }

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

        [HttpGet("Asistencia/DetalleRecibo/{idNomina}")]
        public async Task<IActionResult> DetalleRecibo(int idNomina)
        {
            int userId = GetUserId();
            var planillas = await _context.Planilla
                .Include(p => p.Nomina)
                .Where(p => p.Id_Usr == userId && p.Id_Nomina == idNomina && p.Nomina.Estado != "Anulada")
                .ToListAsync();

            if (!planillas.Any())
            {
                TempData["Error"] = "Recibo no encontrado.";
                return RedirectToAction("Index");
            }

            var nomina = planillas.First().Nomina;

            var deduccionesLey = await _context.DeduccionLey.Where(d => d.Activo).ToListAsync();

            decimal totalBruto = planillas.Sum(p => p.SalarioBruto);
            decimal totalDeducciones = planillas.Sum(p => p.Deducciones);
            decimal totalNeto = planillas.Sum(p => p.SalarioNeto);

            var detalleDto = new VentaFacil.web.Models.Dto.PlanillaDetalleItemDto
            {
                SalarioBruto = totalBruto,
                Deducciones = totalDeducciones,
                SalarioNeto = totalNeto,
                DeduccionesDetalle = deduccionesLey.Select(d => new VentaFacil.web.Models.Dto.DeduccionDetalleItemDto
                {
                    Nombre = d.Nombre,
                    Porcentaje = d.Porcentaje,
                    Monto = totalBruto * (d.Porcentaje / 100m)
                }).ToList(),
                DiasLaborados = planillas.Select(p => new VentaFacil.web.Models.Dto.PlanillaDiaDto
                {
                    FechaInicio = p.FechaInicio,
                    FechaFinal = p.FechaFinal,
                    HorasTrabajadas = p.HorasTrabajadas,
                    HorasExtras = p.HorasExtras,
                    EsFeriado = p.EsFeriado,
                    SalarioBruto = p.SalarioBruto
                }).ToList()
            };

            ViewBag.Nomina = nomina;

            return View(detalleDto);
        }

        [HttpGet("Asistencia/DescargarReciboPdf/{idNomina}")]
        public async Task<IActionResult> DescargarReciboPdf(int idNomina)
        {
            var _planillaService = HttpContext.RequestServices.GetService(typeof(VentaFacil.web.Services.Planilla.IPlanillaService)) as VentaFacil.web.Services.Planilla.IPlanillaService;
            var _pdfService = HttpContext.RequestServices.GetService(typeof(VentaFacil.web.Services.PDF.IPdfService)) as VentaFacil.web.Services.PDF.IPdfService;

            var data = await _planillaService.ObtenerDetalleNominaParaExportarAsync(idNomina);
            if (data == null) return NotFound();

            // Validar que la nómina pertenezca al usuario (o sea admin)
            int userId = GetUserId();
            bool esAdmin = User.IsInRole("Administrador");
            
            // Como las nóminas ahora son individuales, revisamos si algún detalle pertenece al usuario
            bool esPropia = data.Detalles.Any(d => d.Identificacion == User.Identity.Name || (User.FindFirstValue(ClaimTypes.Email) == d.Identificacion));
            // También podemos validar por ID de usuario si tuviéramos esa info en el DTO, pero usemos identificación/correo
            
            if (!esAdmin && !esPropia)
            {
                return Forbid();
            }

            var pdfBytes = _pdfService.GenerarReciboPagoPdf(data);
            string fileName = $"Recibo_Pago_#{idNomina}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpPost("Asistencia/MarcarSalida")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarSalida(bool esFeriado = false)
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
            jornada.EsFeriado = esFeriado;

            var configuracion = await _context.ConfiguracionPlanilla.FirstOrDefaultAsync(c => c.Id_Usr == userId);
            decimal tarifaPorHora = configuracion?.TarifaPorHora ?? 2500m;

            decimal multiplicador = esFeriado ? 2.0m : 1.0m;
            jornada.SalarioBruto = ((jornada.HorasTrabajadas * tarifaPorHora) + (jornada.HorasExtras * tarifaPorHora * 1.5m)) * multiplicador;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Salida registrada. Horas computadas: {horasNormales.ToString("0.##")}h normales y {horasExtras.ToString("0.##")}h extras.";
            return RedirectToAction("Index");
        }
    }
}
