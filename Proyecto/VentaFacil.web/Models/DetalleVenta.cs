using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("DetalleVenta")]
    public class DetalleVenta
    {
        [Key]
        public int Id_Detalle { get; set; }

        public int Id_Venta { get; set; }

        public int Id_Producto { get; set; }

        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioUnitario { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Descuento { get; set; }

        // Navigation properties
        [ForeignKey("Id_Venta")]
        public virtual Venta Venta { get; set; }

        [ForeignKey("Id_Producto")]
        public virtual Producto Producto { get; set; }
    }
}