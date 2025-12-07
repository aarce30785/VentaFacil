using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("Venta")]
    public class Venta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id_Venta")]
        public int Id_Venta { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        [Column("Total", TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        [Column("MetodoPago")]
        public string MetodoPago { get; set; }

        [Column("Estado")]
        public bool Estado { get; set; }

        public int Id_Usuario { get; set; } 

     
        [ForeignKey("Id_Usuario")]
        public virtual Usuario Usuario { get; set; }

        public virtual Factura Factura { get; set; }

        public virtual ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }
}
