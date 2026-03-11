using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response;

namespace VentaFacil.web.Services.Planilla
{
    public interface IBonificacionService
    {
        Task<BaseResponse> AgregarBonificacionAsync(BonificacionDto dto, int idUsuarioResponsable);
        Task<BaseResponse> EditarBonificacionAsync(int idBonificacion, BonificacionDto dto, int idUsuarioResponsable);
        Task<BaseResponse> EliminarBonificacionAsync(int idBonificacion, int idUsuarioResponsable);
        Task<IEnumerable<Bonificacion>> ObtenerBonificacionesPorPlanillaAsync(int idPlanilla);
    }
}
