using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models.Dto
{
    public class PedidoItemDto
    {
        [Key]
        [Display(Name = "ID Detalle")]
        public int Id_Detalle { get; set; }

        [Display(Name = "Producto")]
        public int Id_Producto { get; set; }

        [Display(Name = "Nombre del Producto")]
        public string NombreProducto { get; set; } = string.Empty;

        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }

        [Display(Name = "Precio Unitario")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }

        [Display(Name = "Descuento")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Descuento { get; set; } = 0;

        [Display(Name = "Subtotal")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal
        {
            get { return (Cantidad * PrecioUnitario) - Descuento; }
        }
    }
}
