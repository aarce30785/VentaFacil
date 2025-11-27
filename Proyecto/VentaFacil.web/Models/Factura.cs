using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("Factura")]
    public class Factura
    {
        [Key]
        public int Id_Factura { get; set; }

        public int Id_Venta { get; set; }

        public DateTime FechaEmision { get; set; }

        public string? Cliente { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        public bool Estado { get; set; } = true;

       
        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoPagado { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Cambio { get; set; }

        [StringLength(3)]
        public string Moneda { get; set; } = "CRC";

        [StringLength(20)]
        public string MetodoPago { get; set; } = "Efectivo";

        [Column(TypeName = "decimal(10,4)")]
        public decimal? TasaCambio { get; set; }

        
        public byte[]? PdfData { get; set; }

        [MaxLength(255)]
        public string? PdfFileName { get; set; }

        // Navigation properties
        [ForeignKey("Id_Venta")]
        public virtual Venta Venta { get; set; }

       
        public void CalcularCambio()
        {
            decimal totalEnMonedaPago = Total;

            if (Moneda == "USD" && TasaCambio.HasValue)
            {
                totalEnMonedaPago = Total / TasaCambio.Value;
            }

            Cambio = Math.Max(0, MontoPagado - totalEnMonedaPago);
        }
    }
}
