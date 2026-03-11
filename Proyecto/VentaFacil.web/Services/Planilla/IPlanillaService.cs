using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Planilla;
using VentaFacil.web.Models.Response;
namespace VentaFacil.web.Services.Planilla
{
    public interface IPlanillaService
    {
        Task<RegistrarHorasResponse> RegistrarHorasAsync(RegistrarHorasDto dto);
        Task<GenerarNominaResponse> GenerarNominaAsync(GenerarNominaDto dto);
        Task<RegistrarExtrasBonosResponse> RegistrarExtrasBonosAsync(RegistrarExtrasBonosDto dto);
        Task<NominaConsultaResponse> ConsultarNominasAsync(NominaConsultaDto filtros);
        Task<IEnumerable<PlanillaListadoDto>> ObtenerPlanillasParaExtrasAsync();
        Task<IEnumerable<NominaListadoDto>> ObtenerNominasGeneradasAsync();
        Task<IEnumerable<VentaFacil.web.Models.Usuario>> ObtenerUsuariosAsync();
        Task<BaseResponse> RevertirNominaAsync(int idNomina);
        Task<NominaDetalleDto> ObtenerDetalleNominaParaExportarAsync(int idNomina);
    }
}