using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.ViewModel
{
    public class DetalleFacturaViewModel
    {
        public FacturaDto Factura { get; set; } = new FacturaDto();
        public int FacturaId { get; set; }
        public string NumeroFactura => Factura?.NumeroFactura ?? "N/A";
        public DateTime FechaEmision => Factura?.FechaEmision ?? DateTime.Now;
        public string Cliente => Factura?.Cliente ?? "N/A";
        public decimal Total => Factura?.Total ?? 0;
        public decimal MontoPagado => Factura?.MontoPagado ?? 0;
        public decimal Cambio => Factura?.Cambio ?? 0;
        public string Moneda => Factura?.Moneda ?? "CRC";
        public string MetodoPago => Factura?.MetodoPago.ToString() ?? "N/A";
        public IEnumerable<ItemFacturaDto> Items => Factura?.Items ?? Enumerable.Empty<ItemFacturaDto>();
    }
}
