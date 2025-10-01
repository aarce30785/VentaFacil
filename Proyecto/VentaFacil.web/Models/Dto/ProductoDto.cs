using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models.Dto
{
    public class ProductoDto
    {
        [Key]
        [Display(Name = "ID Producto")]
        public int Id_Producto { get; set; }

        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Precio")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [Display(Name = "Imagen")]
        public string? Imagen { get; set; }

        [Display(Name = "Stock Mínimo")]
        public int StockMinimo { get; set; }

        [Display(Name = "Estado")]
        public bool Estado { get; set; }

        [Display(Name = "ID Categoría")]
        public int Id_Categoria { get; set; }
    }
}
