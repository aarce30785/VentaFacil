using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VentaFacil.web.Services.Planilla;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Planilla;

namespace VentaFacil.web.Controllers
{
    [Route("planilla")]
    public class PlanillaController : Controller
    {
        private readonly IPlanillaService _planillaService;

        public PlanillaController(IPlanillaService planillaService)
        {
            _planillaService = planillaService;
        }

        [HttpPost("registrar-horas")]
        public async Task<IActionResult> RegistrarHoras([FromBody] RegistrarHorasDto dto)
        {
            var response = await _planillaService.RegistrarHorasAsync(dto);
            return Json(response);
        }

        [HttpPost("registrar-extras")]
        public async Task<IActionResult> RegistrarExtras([FromBody] RegistrarExtrasBonosDto dto)
        {
            var response = await _planillaService.RegistrarExtrasBonosAsync(dto);
            return Json(response);
        }

        [HttpPost("generar-nomina")]
        public async Task<IActionResult> GenerarNomina([FromBody] GenerarNominaDto dto)
        {
            var response = await _planillaService.GenerarNominaAsync(dto);
            return Json(response);
        }

        [HttpPost("aplicar-deducciones")]
        public async Task<IActionResult> AplicarDeducciones([FromBody] AplicarDeduccionesDto dto)
        {
            var response = await _planillaService.AplicarDeduccionesAsync(dto);
            return Json(response);
        }

        [HttpGet("consultar")]
        public async Task<IActionResult> Consultar([FromQuery] NominaConsultaDto filtros)
        {
            var response = await _planillaService.ConsultarNominasAsync(filtros);
            return Json(response);
        }
    }
}