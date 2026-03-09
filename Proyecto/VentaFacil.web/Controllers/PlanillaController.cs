using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services.Planilla;
using VentaFacil.web.Services.PDF;

using System.Security.Claims;

namespace VentaFacil.web.Controllers
{
    [Authorize]
    [Route("Planilla")]
    public class PlanillaController : Controller
    {
        private readonly IPlanillaService _planillaService;
        private readonly IBonificacionService _bonificacionService;
        private readonly IPdfService _pdfService;

        public PlanillaController(IPlanillaService planillaService, IBonificacionService bonificacionService, IPdfService pdfService)
        {
            _planillaService = planillaService;
            _bonificacionService = bonificacionService;
            _pdfService = pdfService;
        }

        // ============================
        // INDEX
        // ============================
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ============================
        // CONSULTAR NÓMINAS
        // ============================
        [HttpGet("Consultar")]
        public async Task<IActionResult> Consultar(NominaConsultaDto filtros)
        {
            if (filtros == null) filtros = new NominaConsultaDto();

            if (filtros.Pagina <= 0) filtros.Pagina = 1;
            if (filtros.CantidadPorPagina <= 0) filtros.CantidadPorPagina = 10;

            var response = await _planillaService.ConsultarNominasAsync(filtros);

            if (!response.Success)
                TempData["Error"] = response.Message;
            
            // Mantener filtros en la vista
            ViewBag.Filtros = filtros;
            await CargarUsuariosViewBag(filtros.Id_Usr);

            return View("~/Views/Planilla/Consultar.cshtml", response);
        }

        // ============================
        // EXPORTAR NÓMINA
        // ============================
        [HttpGet("ExportarNomina")]
        public async Task<IActionResult> ExportarNomina(int idNomina)
        {
            var detalle = await _planillaService.ObtenerDetalleNominaParaExportarAsync(idNomina);
            if (detalle == null)
            {
                TempData["Error"] = "Nómina no encontrada.";
                return RedirectToAction("Consultar");
            }

            var pdfBytes = _pdfService.GenerarReporteNomina(detalle);
            string fileName = $"Nomina_{detalle.FechaInicio:yyyyMMdd}_{detalle.FechaFinal:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet("ExportarNominaExcel")]
        public async Task<IActionResult> ExportarNominaExcel(int idNomina)
        {
            var detalle = await _planillaService.ObtenerDetalleNominaParaExportarAsync(idNomina);
            if (detalle == null)
            {
                TempData["Error"] = "Nómina no encontrada.";
                return RedirectToAction("Consultar");
            }

            var excelBytes = _pdfService.GenerarExcelNomina(detalle);
            string fileName = $"Nomina_{detalle.FechaInicio:yyyyMMdd}_{detalle.FechaFinal:yyyyMMdd}.xlsx";

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ============================
        // REGISTRAR HORAS
        // ============================
        [HttpGet("RegistrarHoras")]
        public async Task<IActionResult> RegistrarHoras()
        {
            var modelo = new RegistrarHorasDto
            {
                FechaInicio = System.DateTime.Now,
                FechaFinal = System.DateTime.Now.AddHours(8)
            };

            await CargarUsuariosViewBag();

            return View("~/Views/Planilla/RegistrarHoras.cshtml", modelo);
        }

        [HttpPost("RegistrarHoras")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarHoras(RegistrarHorasDto dto)
        {
            if (!ModelState.IsValid)
            {
                await CargarUsuariosViewBag(dto.Id_Usr);
                return View("~/Views/Planilla/RegistrarHoras.cshtml", dto);
            }

            var response = await _planillaService.RegistrarHorasAsync(dto);

            if (!response.Success)
            {
                TempData["Error"] = response.Message;
                await CargarUsuariosViewBag(dto.Id_Usr);
                return View("~/Views/Planilla/RegistrarHoras.cshtml", dto);
            }

            TempData["Success"] = response.Message;
            return RedirectToAction("Consultar");
        }

        // ============================
        // REGISTRAR EXTRAS Y BONOS
        // ============================
        [HttpGet("RegistrarExtrasBonos")]
        public async Task<IActionResult> RegistrarExtrasBonos()
        {
            var planillas = await _planillaService.ObtenerPlanillasParaExtrasAsync();

            ViewBag.Planillas = new SelectList(
                planillas,
                "Id_Planilla",
                "InfoCompleta"
            );

            return View("~/Views/Planilla/RegistrarExtrasBonos.cshtml", new RegistrarExtrasBonosDto());
        }

        [HttpPost("RegistrarExtrasBonos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarExtrasBonos(RegistrarExtrasBonosDto dto)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || Request.Headers["Accept"].ToString().Contains("application/json");

            if (!ModelState.IsValid)
            {
                if (isAjax)
                    return Json(new { success = false, message = "Datos inv\u00e1lidos." });

                var planillas = await _planillaService.ObtenerPlanillasParaExtrasAsync();
                ViewBag.Planillas = new SelectList(planillas, "Id_Planilla", "InfoCompleta");
                return View("~/Views/Planilla/RegistrarExtrasBonos.cshtml", dto);
            }

            var response = await _planillaService.RegistrarExtrasBonosAsync(dto);

            if (isAjax)
                return Json(new { success = response.Success, message = response.Message });

            if (!response.Success)
            {
                TempData["Error"] = response.Message;
                var planillas = await _planillaService.ObtenerPlanillasParaExtrasAsync();
                ViewBag.Planillas = new SelectList(planillas, "Id_Planilla", "InfoCompleta");
                return View("~/Views/Planilla/RegistrarExtrasBonos.cshtml", dto);
            }

            TempData["Success"] = response.Message;
            return RedirectToAction("Consultar");
        }

        // ============================
        // GENERAR NÓMINA (solo Administrador)
        // ============================
        [HttpGet("GenerarNomina")]
        [Authorize(Roles = "Administrador")]
        public IActionResult GenerarNomina()
        {
            // Por defecto: semana actual (Lunes - Domingo)
            var hoy = System.DateTime.Today;
            int diasDesdeElLunes = ((int)hoy.DayOfWeek + 6) % 7; // 0=lunes
            var lunes   = hoy.AddDays(-diasDesdeElLunes);
            var domingo = lunes.AddDays(6);

            var modelo = new GenerarNominaDto
            {
                TipoPeriodo               = "Semanal",
                FechaInicio               = lunes,
                FechaFinal                = domingo,
                IncluirSoloUsuariosActivos = true
            };

            return View("~/Views/Planilla/GenerarNomina.cshtml", modelo);
        }

        [HttpPost("GenerarNomina")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarNomina(GenerarNominaDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Planilla/GenerarNomina.cshtml", dto);
            }

            var response = await _planillaService.GenerarNominaAsync(dto);

            if (!response.Success)
            {
                TempData["Error"] = response.Message;
                if (response.ErroresValidacion != null && response.ErroresValidacion.Any())
                {
                    ViewBag.ErroresValidacion = response.ErroresValidacion;
                }
                return View("~/Views/Planilla/GenerarNomina.cshtml", dto);
            }

            TempData["Success"] = response.Message;
            return RedirectToAction("DetalleNomina", new { idNomina = response.Id_Nomina });
        }

        // ============================
        // DETALLE NÓMINA
        // ============================
        [HttpGet("DetalleNomina/{idNomina}")]
        public async Task<IActionResult> DetalleNomina(int idNomina)
        {
            var dto = await _planillaService.ObtenerDetalleNominaParaExportarAsync(idNomina);
            if (dto == null)
            {
                TempData["Error"] = "Nómina no encontrada.";
                return RedirectToAction("Consultar");
            }

            return View("~/Views/Planilla/DetalleNomina.cshtml", dto);
        }

        // ============================
        // REVERTIR NÓMINA (solo Administrador)
        // ============================
        [HttpPost("RevertirNomina")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertirNomina(int idNomina, string justificacion)
        {
            if (string.IsNullOrWhiteSpace(justificacion))
            {
                TempData["Error"] = "Debe proporcionar una justificación para anular la nómina.";
                return RedirectToAction("Consultar");
            }

            var response = await _planillaService.RevertirNominaAsync(idNomina, justificacion);

            if (!response.Success)
            {
                TempData["Error"] = response.Message;
            }
            else
            {
                TempData["Success"] = response.Message;
            }

            return RedirectToAction("Consultar");
        }

        // ============================
        // HISTORIAL LABORAL (5 AÑOS)
        // ============================
        [HttpGet("HistorialLaboral")]
        public async Task<IActionResult> HistorialLaboral(int? idUsuario, int pagina = 1)
        {
            bool esAdmin = User.IsInRole("Administrador");

            // Si no es admin, forzar a ver solo el propio historial
            int usuarioFinal = esAdmin && idUsuario.HasValue
                ? idUsuario.Value
                : GetUserId();

            var resultado = await _planillaService.ObtenerHistorialUsuarioAsync(usuarioFinal, pagina);

            if (!resultado.Success)
                TempData["Error"] = resultado.Message;

            ViewBag.EsAdmin  = esAdmin;
            if (esAdmin) await CargarUsuariosViewBag(usuarioFinal);

            return View("~/Views/Planilla/HistorialLaboral.cshtml", resultado);
        }

        // ============================
        // BONIFICACIONES
        // ============================
        [HttpGet("ObtenerBonificaciones")]
        public async Task<IActionResult> ObtenerBonificaciones(int idPlanilla)
        {
            var bonificaciones = await _bonificacionService.ObtenerBonificacionesPorPlanillaAsync(idPlanilla);
            var data = bonificaciones.Select(b => new {
                b.Id,
                b.Id_Planilla,
                b.Monto,
                b.Motivo,
                Fecha = b.Fecha.ToString("yyyy-MM-dd"),
                FechaRegistro = b.FechaRegistro.ToString("yyyy-MM-dd HH:mm")
            });
            return Json(new { success = true, data });
        }

        [HttpPost("GuardarBonificacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarBonificacion([FromBody] BonificacionDto dto)
        {
            int idUsuario = GetUserId(); 

            var response = await _bonificacionService.AgregarBonificacionAsync(dto, idUsuario);
            return Json(response);
        }

        [HttpPost("EditarBonificacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarBonificacion(int id, [FromBody] BonificacionDto dto)
        {
            int idUsuario = GetUserId();

            var response = await _bonificacionService.EditarBonificacionAsync(id, dto, idUsuario);
            return Json(response);
        }

        [HttpPost("EliminarBonificacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarBonificacion(int id)
        {
            int idUsuario = GetUserId();

            var response = await _bonificacionService.EliminarBonificacionAsync(id, idUsuario);
            return Json(response);
        }

        [HttpGet("ObtenerAuditoriaBonificacion")]
        public async Task<IActionResult> ObtenerAuditoriaBonificacion(int id)
        {
            var registros = await _bonificacionService.ObtenerAuditoriaBonificacionAsync(id);
            var data = registros.Select(a => new {
                a.Id,
                a.MontoAnterior,
                a.MontoNuevo,
                a.MotivoCambio,
                FechaCambio = a.FechaCambio.ToString("yyyy-MM-dd HH:mm"),
                Responsable = a.UsuarioResponsable?.Nombre ?? $"ID {a.Id_UsuarioResponsable}"
            });
            return Json(new { success = true, data });
        }

        // ============================
        // METODO AUXILIAR
        // ============================
        private async Task CargarUsuariosViewBag(int? selectedId = null)
        {
            var usuarios = await _planillaService.ObtenerUsuariosAsync();
            ViewBag.Usuarios = new SelectList(usuarios, "Id_Usr", "Nombre", selectedId);
        }
        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("UsuarioId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return 0;
        }

    }
}