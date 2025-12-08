using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models;

namespace VentaFacil.web.Services.Caja
{
    public interface ICajaService
    {
        Task<VentaFacil.web.Models.Caja> AbrirCajaAsync(int idUsuario, decimal montoInicial);
        Task<VentaFacil.web.Models.Caja> CerrarCajaAsync(int idCaja);
        Task<CajaRetiro> RegistrarRetiroAsync(int idCaja, int idUsuario, decimal monto, string motivo);
        Task<List<VentaFacil.web.Models.Caja>> ListarCajasAsync(); 
        Task<List<CajaRetiro>> ObtenerRetirosPorCajaAsync(int idCaja);
        Task<decimal> GetIngresosRecientesAsync();
        Task<decimal> GetGastosRecientesAsync();
    }
}
