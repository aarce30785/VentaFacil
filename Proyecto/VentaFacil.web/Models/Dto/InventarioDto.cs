using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class InventarioDto
    {
        [Key]
        [Display(Name = "ID Inventario")]
        public int Id_Inventario { get; set; }

        [Display(Name = "ID Producto")]
        public int Id_Producto { get; set; }

        [Display(Name = "Stock Actual")]
        public int StockActual { get; set; }
    }
}
