using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("Inventario")]
    public class Inventario
    {
        [Key]
        [Column("Id_Inventario")]
        public int Id_Inventario { get; set; }

        [Column("Id_Producto")]
        public int Id_Producto { get; set; }

        [Required]
        public int StockActual { get; set; }

        
        // Propiedad de navegación
        [ForeignKey("Id_Producto")]
        public Producto Producto { get; set; }
    }
}
