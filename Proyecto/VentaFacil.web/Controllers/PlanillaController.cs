using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VentaFacil.web.Services.Planilla;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Planilla;

namespace VentaFacil.web.Controllers
{
    [ApiController]
    [Route("api/planilla")]
    public class PlanillaController : ControllerBase
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
            return Ok(response);
        }

        [HttpPost("registrar-extras")]
        public async Task<IActionResult> RegistrarExtras([FromBody] RegistrarExtrasBonosDto dto)
        {
            var response = await _planillaService.RegistrarExtrasBonosAsync(dto);
            return Ok(response);
        }

        [HttpPost("generar-nomina")]
        public async Task<IActionResult> GenerarNomina([FromBody] GenerarNominaDto dto)
        {
            var response = await _planillaService.GenerarNominaAsync(dto);
            return Ok(response);
        }

        [HttpPost("aplicar-deducciones")]
        public async Task<IActionResult> AplicarDeducciones([FromBody] AplicarDeduccionesDto dto)
        {
            var response = await _planillaService.AplicarDeduccionesAsync(dto);
            return Ok(response);
        }

        [HttpGet("consultar")]
        public async Task<IActionResult> Consultar([FromQuery] NominaConsultaDto filtros)
        {
            var response = await _planillaService.ConsultarNominasAsync(filtros);
            return Ok(response);
        }
    }
}