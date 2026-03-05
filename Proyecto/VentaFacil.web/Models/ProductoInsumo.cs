using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("ProductoInsumo")]
    public class ProductoInsumo
    {
        [Key]
        [Column("Id_ProductoInsumo")]
        public int Id_ProductoInsumo { get; set; }

        [Required]
        [Column("Id_Producto")]
        public int Id_Producto { get; set; }

        [Required]
        [Column("Id_Inventario")]
        public int Id_Inventario { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [ForeignKey("Id_Producto")]
        public virtual Producto? Producto { get; set; }

        [ForeignKey("Id_Inventario")]
        public virtual Inventario? Inventario { get; set; }
    }
}
