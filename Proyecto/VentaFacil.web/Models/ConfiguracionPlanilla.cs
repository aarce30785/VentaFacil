using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    public class ConfiguracionPlanilla
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id_Configuracion { get; set; }

        [Required]
        [ForeignKey("Usuario")]
        public int Id_Usr { get; set; }

        public virtual Usuario Usuario { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TarifaPorHora { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
