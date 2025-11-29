namespace VentaFacil.web.Models.Dto
{
    public class PagoFacturaDto
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public string MetodoPago { get; set; }
        public decimal Monto { get; set; }
        public string Moneda { get; set; }
        public decimal? TasaCambio { get; set; }
    }
}
