using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services.Planilla;
using VentaFacil.web.Services.PDF;

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
                await CargarUsuariosViewBag();
                return View("~/Views/Planilla/RegistrarHoras.cshtml", dto);
            }

            var response = await _planillaService.RegistrarHorasAsync(dto);

            if (!response.Success)
            {
                TempData["Error"] = response.Message;
                await CargarUsuariosViewBag();
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
                "Periodo"
            );

            return View("~/Views/Planilla/RegistrarExtrasBonos.cshtml", new RegistrarExtrasBonosDto());
        }

        [HttpPost("RegistrarExtrasBonos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarExtrasBonos(RegistrarExtrasBonosDto dto)
        {
            if (!ModelState.IsValid)
            {
                var planillas = await _planillaService.ObtenerPlanillasParaExtrasAsync();
                ViewBag.Planillas = new SelectList(planillas, "Id_Planilla", "Periodo");

                return View("~/Views/Planilla/RegistrarExtrasBonos.cshtml", dto);
            }

            var response = await _planillaService.RegistrarExtrasBonosAsync(dto);

            if (!response.Success)
            {
                TempData["Error"] = response.Message;

                var planillas = await _planillaService.ObtenerPlanillasParaExtrasAsync();
                ViewBag.Planillas = new SelectList(planillas, "Id_Planilla", "Periodo");

                return View("~/Views/Planilla/RegistrarExtrasBonos.cshtml", dto);
            }

            TempData["Success"] = response.Message;
            return RedirectToAction("Consultar");
        }

        // ============================
        // GENERAR NÓMINA
        // ============================
        [HttpGet("GenerarNomina")]
        public IActionResult GenerarNomina()
        {
            var modelo = new GenerarNominaDto
            {
                FechaInicio = System.DateTime.Today,
                FechaFinal = System.DateTime.Today,
                IncluirSoloUsuariosActivos = true
            };

            return View("~/Views/Planilla/GenerarNomina.cshtml", modelo);
        }

        [HttpPost("GenerarNomina")]
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
                return View("~/Views/Planilla/GenerarNomina.cshtml", dto);
            }

            TempData["Success"] = response.Message;
            return RedirectToAction("Consultar");
        }

        // ============================
        // REVERTIR NÓMINA
        // ============================
        [HttpPost("RevertirNomina")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertirNomina(int idNomina)
        {
            var response = await _planillaService.RevertirNominaAsync(idNomina);

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
        // BONIFICACIONES
        // ============================
        [HttpGet("ObtenerBonificaciones")]
        public async Task<IActionResult> ObtenerBonificaciones(int idPlanilla)
        {
            var bonificaciones = await _bonificacionService.ObtenerBonificacionesPorPlanillaAsync(idPlanilla);
            return Json(new { success = true, data = bonificaciones });
        }

        [HttpPost("GuardarBonificacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarBonificacion([FromBody] BonificacionDto dto)
        {
            // TODO: Obtener ID de usuario real de la sesión
            int idUsuario = 1; 

            var response = await _bonificacionService.AgregarBonificacionAsync(dto, idUsuario);
            return Json(response);
        }

        [HttpPost("EditarBonificacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarBonificacion(int id, [FromBody] BonificacionDto dto)
        {
            // TODO: Obtener ID de usuario real de la sesión
            int idUsuario = 1;

            var response = await _bonificacionService.EditarBonificacionAsync(id, dto, idUsuario);
            return Json(response);
        }

        [HttpPost("EliminarBonificacion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarBonificacion(int id)
        {
            // TODO: Obtener ID de usuario real de la sesión
            int idUsuario = 1;

            var response = await _bonificacionService.EliminarBonificacionAsync(id, idUsuario);
            return Json(response);
        }

        // ============================
        // METODO AUXILIAR
        // ============================
        private async Task CargarUsuariosViewBag()
        {
            var usuarios = await _planillaService.ObtenerUsuariosAsync();
            ViewBag.Usuarios = new SelectList(usuarios, "Id_Usr", "Nombre");
        }
    }
}