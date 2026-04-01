using System.Collections.Generic;
using System.Threading.Tasks;
using VentaFacil.web.Models;

namespace VentaFacil.web.Services.Caja
{
    public interface ICajaService
    {
        Task<VentaFacil.web.Models.Caja> AbrirCajaAsync(int idUsuario, decimal montoInicial, decimal montoInicialUSD);
        Task<VentaFacil.web.Models.Caja> CerrarCajaAsync(int idCaja, int idUsuario, decimal montoFisico, decimal montoFisicoUSD, string justificacion);
        Task<CajaRetiro> RegistrarIngresoAsync(int idCaja, int idUsuario, decimal monto, string motivo, string moneda = "CRC");
        Task<CajaRetiro> RegistrarRetiroAsync(int idCaja, int idUsuario, decimal monto, string motivo, string moneda = "CRC");
        Task<VentaFacil.web.Models.Response.Caja.ListCajaResponse> ListarCajasAsync(int pagina = 1, int cantidadPorPagina = 10); 
        Task<List<CajaRetiro>> ObtenerRetirosPorCajaAsync(int idCaja);
        Task<decimal> GetIngresosRecientesAsync();
        Task<decimal> GetGastosRecientesAsync();
        Task<bool> ExisteCajaAbiertaAsync(int? idUsuario = null);
        Task<VentaFacil.web.Models.Caja> ObtenerCajaPorIdAsync(int idCaja);
        Task CerrarCajasExcedidasAsync();
    }
}
