using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Planilla;

namespace VentaFacil.web.Services.Planilla
{
    public interface IPlanillaService
    {
        Task<RegistrarHorasResponse> RegistrarHorasAsync(RegistrarHorasDto dto);
        Task<GenerarNominaResponse> GenerarNominaAsync(GenerarNominaDto dto);
        Task<AplicarDeduccionesResponse> AplicarDeduccionesAsync(AplicarDeduccionesDto dto);
        Task<RegistrarExtrasBonosResponse> RegistrarExtrasBonosAsync(RegistrarExtrasBonosDto dto);
        Task<NominaConsultaResponse> ConsultarNominasAsync(NominaConsultaDto filtros);
    }
}