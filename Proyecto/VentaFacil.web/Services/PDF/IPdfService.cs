using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response;

namespace VentaFacil.web.Services.PDF
{
    public interface IPdfService
    {
        byte[] GenerarFacturaPdf(FacturaDto facturaDto, bool esCopia = false);
        byte[] GenerarHistorialMovimientosPdf(List<InventarioMovimientoDto> movimientos, string nombreInsumo);
        byte[] GenerarReporteNomina(NominaDetalleDto data);
    }
}
