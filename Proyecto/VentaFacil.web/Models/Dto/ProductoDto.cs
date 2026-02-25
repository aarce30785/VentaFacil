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
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Descripcion { get; set; }

        [Display(Name = "Precio")]
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Display(Name = "Imagen")]
        public string? Imagen { get; set; }

        [Display(Name = "Stock Mínimo")]
        [Required(ErrorMessage = "El stock mínimo es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "El stock mínimo debe ser al menos 1")]
        public int StockMinimo { get; set; }

        [Display(Name = "Stock Actual")]
        [Required(ErrorMessage = "El stock actual es obligatorio")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock actual no puede ser negativo")]
        public int StockActual { get; set; }

        [Display(Name = "Estado")]
        public bool Estado { get; set; }

        [Display(Name = "ID Categoría")]
        [Required(ErrorMessage = "La categoría es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una categoría válida")]
        public int Id_Categoria { get; set; }
    }
}
