using VentaFacil.web.Models.Enum;

namespace VentaFacil.web.Models.Dto
{
    public class FacturaDto
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime FechaEmision { get; set; }
        public string Cliente { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Impuestos { get; set; }
        public decimal Total { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public decimal MontoPagado { get; set; }
        public string Moneda { get; set; } = "CRC";
        public decimal Cambio { get; set; }
        public decimal? TasaCambio { get; set; }
        public decimal? TotalColones { get; set; }
        public decimal? TotalDolares { get; set; }
        public EstadoFactura EstadoFactura { get; set; }
        public List<ItemFacturaDto> Items { get; set; } = new List<ItemFacturaDto>();
    }
}
