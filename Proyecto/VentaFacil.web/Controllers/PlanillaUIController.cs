using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Services.Planilla;

namespace VentaFacil.web.Controllers
{
    [Route("Planilla")]
    public class PlanillaUIController : Controller
    {
        private readonly IPlanillaService _planillaService;

        public PlanillaUIController(IPlanillaService planillaService)
        {
            _planillaService = planillaService;
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

            return View("~/Views/Planilla/Consultar.cshtml", response);
        }

        // ============================
        // REGISTRAR HORAS
        // ============================
        [HttpGet("RegistrarHoras")]
        public IActionResult RegistrarHoras()
        {
            var modelo = new RegistrarHorasDto
            {
                FechaInicio = System.DateTime.Today,
                FechaFinal = System.DateTime.Today
            };

            CargarUsuariosViewBag();

            return View("~/Views/Planilla/RegistrarHoras.cshtml", modelo);
        }

        [HttpPost("RegistrarHoras")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarHoras(RegistrarHorasDto dto)
        {
            if (!ModelState.IsValid)
            {
                CargarUsuariosViewBag();
                return View("~/Views/Planilla/RegistrarHoras.cshtml", dto);
            }

            var response = await _planillaService.RegistrarHorasAsync(dto);

            if (!response.Success)
            {
                TempData["Error"] = response.Message;
                CargarUsuariosViewBag();
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
                "Periodo"   // ← CAMBIO AQUI
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
                ViewBag.Planillas = new SelectList(planillas, "Id_Planilla", "Periodo"); // ← CAMBIO AQUÍ

                return View("~/Views/Planilla/RegistrarExtrasBonos.cshtml", dto);
            }

            var response = await _planillaService.RegistrarExtrasBonosAsync(dto);

            if (!response.Success)
            {
                TempData["Error"] = response.Message;

                var planillas = await _planillaService.ObtenerPlanillasParaExtrasAsync();
                ViewBag.Planillas = new SelectList(planillas, "Id_Planilla", "Periodo"); // ← CAMBIO AQUÍ

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
        // APLICAR DEDUCCIONES
        // ============================
        [HttpGet("AplicarDeducciones")]
        public async Task<IActionResult> AplicarDeducciones()
        {
            // Obtener lista de nóminas generadas con descripción lista
            var nominas = await _planillaService.ObtenerNominasGeneradasAsync();

            ViewBag.Nominas = new SelectList(
                nominas,
                "Id_Nomina",
                "Descripcion"
            );

            return View("~/Views/Planilla/AplicarDeducciones.cshtml", new AplicarDeduccionesDto());
        }

        [HttpPost("AplicarDeducciones")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AplicarDeducciones(AplicarDeduccionesDto dto)
        {
            if (!ModelState.IsValid)
            {
                var nominas = await _planillaService.ObtenerNominasGeneradasAsync();

                ViewBag.Nominas = new SelectList(
                    nominas,
                    "Id_Nomina",
                    "Descripcion"
                );

                return View("~/Views/Planilla/AplicarDeducciones.cshtml", dto);
            }

            var response = await _planillaService.AplicarDeduccionesAsync(dto);

            if (!response.Success)
            {
                TempData["Error"] = response.Message;

                var nominas = await _planillaService.ObtenerNominasGeneradasAsync();

                ViewBag.Nominas = new SelectList(
                    nominas,
                    "Id_Nomina",
                    "Descripcion"
                );

                return View("~/Views/Planilla/AplicarDeducciones.cshtml", dto);
            }

            TempData["Success"] = response.Message;
            return RedirectToAction("Consultar");
        }

        // ============================
        // METODO AUXILIAR
        // ============================
        private void CargarUsuariosViewBag()
        {
            ViewBag.Usuarios = new SelectList(
                new List<object>
                {
                    new { Id_Usr = 1, Nombre = "Usuario de Prueba" },
                    new { Id_Usr = 2, Nombre = "Admin Sistema" },
                    new { Id_Usr = 3, Nombre = "Vendedor Local" }
                },
                "Id_Usr",
                "Nombre"
            );
        }
    }
}
