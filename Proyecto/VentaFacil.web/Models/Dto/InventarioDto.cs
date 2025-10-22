using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class InventarioDto
    {
        [Key]
        [Display(Name = "ID Inventario")]
        public int Id_Inventario { get; set; }

        [MaxLength(255)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Display(Name = "Stock Actual")]
        public int StockActual { get; set; }

        [Display(Name = "Stock Mínimo")]
        public int StockMinimo { get; set; }
    }
}
