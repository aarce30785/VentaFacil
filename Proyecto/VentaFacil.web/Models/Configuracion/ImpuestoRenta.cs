using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models.Configuracion
{
    [Table("ImpuestoRenta")]
    public class ImpuestoRenta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Anio { get; set; } // 2025

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal LimiteInferior { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? LimiteSuperior { get; set; } // Null para el último tramo (Exceso de...)

        [Required]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal Porcentaje { get; set; } // 0, 10, 15, 20, 25
    }
}
