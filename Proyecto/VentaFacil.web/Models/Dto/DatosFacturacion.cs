using VentaFacil.web.Models.Enum;

namespace VentaFacil.web.Models.Dto
{
    public class DatosFacturacion
    {
        public int PedidoId { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public decimal MontoPagado { get; set; }
        public string Moneda { get; set; } = "CRC";
        public decimal? TasaCambio { get; set; }
        public decimal Cambio { get; set; }
    }
}
