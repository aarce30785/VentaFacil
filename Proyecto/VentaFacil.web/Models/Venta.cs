using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("Venta")]
    public class Venta
    {
        [Key]
        [Column("Id_Venta")]
        public int Id_Venta { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string MetodoPago { get; set; }
        public bool Estado { get; set; }

        [Column("Id_Usuario")]  // ← MAPEAR A LA COLUMNA CORRECTA
        public int? Id_Usuario { get; set; }

        // Navigation properties
        [ForeignKey("Id_Usuario")]
        public virtual Usuario? Usuario { get; set; }
        public virtual Factura Factura { get; set; }
        public virtual ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();


    }
}
