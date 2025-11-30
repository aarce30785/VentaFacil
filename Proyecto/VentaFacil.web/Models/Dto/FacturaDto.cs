using VentaFacil.web.Models.Enum;

namespace VentaFacil.web.Models.Dto
{
    public class FacturaDto
    {
        public int Id_Factura { get; set; }
        public int PedidoId { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime FechaEmision { get; set; }
        public string Cliente { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }
        public MetodoPago MetodoPago { get; set; }

        // ✅ INCLUIR TODOS LOS CAMPOS DE PAGO
        public decimal MontoPagado { get; set; }
        public decimal Cambio { get; set; }
        public string Moneda { get; set; } = "CRC";
        public decimal? TasaCambio { get; set; }

        // Campos para conversión USD
        public decimal? TotalColones { get; set; }
        public decimal? TotalDolares { get; set; }

        public EstadoFactura EstadoFactura { get; set; }
        public List<ItemFacturaDto> Items { get; set; } = new();
        public List<PagoFacturaDto> Pagos { get; set; } = new();
    }
}
