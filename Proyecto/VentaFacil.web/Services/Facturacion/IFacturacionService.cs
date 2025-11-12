using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Response.Factura;

namespace VentaFacil.web.Services.Facturacion
{
    public interface IFacturacionService
    {
        Task<ResultadoFacturacion> GenerarFacturaAsync(int pedidoId, MetodoPago metodoPago, decimal montoPagado, string moneda = "CRC");
        Task<ResultadoFacturacion> GenerarFacturaDolaresAsync(int pedidoId, decimal montoPagado, decimal tasaCambio);
        Task<FacturaDto> ObtenerFacturaAsync(int facturaId);
    }
    
}
