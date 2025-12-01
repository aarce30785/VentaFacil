using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("InventarioMovimiento")]
    public class InventarioMovimiento
    {
        [Key]
        [Column("Id_Movimiento")]
        public int Id_Movimiento { get; set; }

        [Required]
        [Column("Id_Inventario")]
        public int Id_Inventario { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("Tipo_Movimiento")]
        public string Tipo_Movimiento { get; set; }

        [Required]
        [Column("Cantidad")]
        public int Cantidad { get; set; }

        [Required]
        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        [Required]
        [Column("Id_Usuario")]
        public int Id_Usuario { get; set; }

        // Propiedad de navegación
        [ForeignKey("Id_Inventario")]
        public Inventario Inventario { get; set; }

        public string Observaciones { get; set; }
    }
}
