namespace VentaFacil.web.Models.Response
{
    public class FacturaResponse
    {
        public int Id_Factura { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime FechaEmision { get; set; }
        public string? Cliente { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public List<FacturaItemResponse> Items { get; set; } = new List<FacturaItemResponse>();
        public PagoInfoResponse Pago { get; set; }
    }

    public class FacturaItemResponse
    {
        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public decimal? Descuento { get; set; }
    }

    public class PagoInfoResponse
    {
        public string MetodoPago { get; set; }
        public decimal MontoPagado { get; set; }
        public string Moneda { get; set; }
        public DateTime FechaPago { get; set; }
    }
}
