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

        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        public bool Estado { get; set; } = true;

        // Navigation properties
        [ForeignKey("Id_Venta")]
        public virtual Venta Venta { get; set; }
    }
}
