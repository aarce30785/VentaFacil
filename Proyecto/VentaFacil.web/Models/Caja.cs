using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    public class Caja
    {
        [Key]
        public int Id_Caja { get; set; }

        [Required]
        public int Id_Usuario { get; set; }

        [Required]
        public DateTime Fecha_Apertura { get; set; }

        public DateTime? Fecha_Cierre { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto_Inicial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Monto { get; set; }

        [Required]
        [MaxLength(50)]
        public string Estado { get; set; } = string.Empty;
    }
}
