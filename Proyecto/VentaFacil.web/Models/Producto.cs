using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("Producto")]
    public class Producto
    {
        [Key]
        [Column("Id_Producto")]
        public int Id_Producto { get; set; }

        [Required]
        [MaxLength(255)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string? Descripcion { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }

        [MaxLength(2048)]
        public string? Imagen { get; set; }

        [Required]
        public int StockMinimo { get; set; }

        [Required]
        public int StockActual { get; set; }

        [Required]
        public bool Estado { get; set; } = true;

        
        [Column("Id_Categoria")]
        public int Id_Categoria { get; set; }
    }
}
