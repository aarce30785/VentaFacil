using System.Threading.Tasks;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Response.Factura;

namespace VentaFacil.web.Services.Facturacion
{
    public interface IFacturacionService
    {
        Task<ResultadoFacturacion> GenerarFacturaAsync(int pedidoId, MetodoPago metodoPago, decimal montoPagado, string moneda = "CRC");
        Task<ResultadoFacturacion> GenerarFacturaMixtaAsync(int pedidoId, List<PagoFacturaDto> pagos);
        Task<ResultadoFacturacion> GenerarFacturaDolaresAsync(int pedidoId, decimal montoPagado, decimal tasaCambio);
        Task<FacturaDto> ObtenerFacturaAsync(int facturaId);
        Task<List<FacturaDto>> BuscarFacturasAsync(DateTime? fechaInicio, DateTime? fechaFin, int? numeroFactura, string? cliente);
        Task<bool> AnularFacturaAsync(int facturaId, string justificacion);
        Task<int> GenerarNotaCreditoAsync(int facturaId, List<int> productosIds);
        (bool EsValido, string Mensaje) ValidarMontoPago(decimal totalPedido, decimal montoPagado, string moneda, decimal? tasaCambio);
        Task<decimal> GetVentasDiaAsync();
        Task<decimal> GetVentasSemanaAsync();
        Task<decimal> GetVentasMesAsync();
    }
    
}
