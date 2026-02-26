using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    using VentaFacil.web.Models.Enum;
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

        /// <summary>Verdadero cuando el stock mínimo no está configurado (= 0). No persiste en BD.</summary>
        public bool SinStockMinimo => StockMinimo <= 0;

        [Required(ErrorMessage = "La unidad de medida es requerida")]
        [Display(Name = "Unidad de Medida")]
        public UnidadMedida UnidadMedida { get; set; }

        public bool Estado { get; set; } = true;
    }
}
