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

        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        [Column("Total", TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        // En el SQL existe; para PE1 no lo usaremos (puede quedar null)
        [Column("MetodoPago")]
        public string? MetodoPago { get; set; }

        // En tu tabla es BIT; lo mapeamos a bool (false = pendiente)
        [Column("Estado")]
        public bool Estado { get; set; }

        [Column("Id_Usuario")]
        public int Id_Usuario { get; set; }
    }
}
