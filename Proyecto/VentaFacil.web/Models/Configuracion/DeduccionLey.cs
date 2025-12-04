using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models.Configuracion
{
    [Table("DeduccionLey")]
    public class DeduccionLey
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } // SEM, IVM, LPT

        [Required]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal Porcentaje { get; set; } // Ejemplo: 5.50

        public bool Activo { get; set; } = true;
    }
}
