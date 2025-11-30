using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VentaFacil.web.Models.Enum;

namespace VentaFacil.web.Models
{
    [Table("Factura")]
    public class Factura
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id_Factura { get; set; }

        public int Id_Venta { get; set; }

        [StringLength(255)]
        public string Cliente { get; set; } = string.Empty;

        public DateTime FechaEmision { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

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
        
        public EstadoFactura Estado { get; set; } = EstadoFactura.Activa;

        public string? Justificacion { get; set; }

        public int? FacturaOriginalId { get; set; }

        public byte[]? PdfData { get; set; }

        [StringLength(255)]
        public string? PdfFileName { get; set; }

        [ForeignKey("Id_Venta")]
        public virtual Venta? Venta { get; set; }

        public virtual ICollection<PagoFactura> Pagos { get; set; } = new List<PagoFactura>();

        public void CalcularCambio()
        {
            Cambio = Math.Max(0, MontoPagado - Total);
        }
    }
}
