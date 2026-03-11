using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response;

namespace VentaFacil.web.Services.PDF
{
    public interface IPdfService
    {
        byte[] GenerarFacturaPdf(FacturaDto facturaDto, object footer = null, bool esCopia = false);
        byte[] GenerarFacturaPdf(FacturaDto facturaDto);
        byte[] GenerarHistorialMovimientosPdf(List<InventarioMovimientoDto> movimientos, string nombreInsumo);
        byte[] GenerarReporteNomina(NominaDetalleDto data);
        byte[] GenerarExcelNomina(NominaDetalleDto data);
        byte[] GenerarArqueoPdf(VentaFacil.web.Models.Caja caja, List<CajaRetiro> retiros);
        byte[] GenerarReporteVentasDiariasPdf(List<VentaFacil.web.Models.Factura> facturas, System.DateTime fecha);
        byte[] GenerarReportePersonalizadoExcel(List<VentaFacil.web.Models.Factura> facturas, System.DateTime fechaInicio, System.DateTime fechaFin);
    }
}
