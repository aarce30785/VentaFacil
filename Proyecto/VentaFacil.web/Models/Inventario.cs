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

        [Required]
        [MaxLength(255)]
        public string Nombre { get; set; }

        [Required]
        public int StockActual { get; set; }

        [Required]
        public int StockMinimo { get; set; }
    }
}
