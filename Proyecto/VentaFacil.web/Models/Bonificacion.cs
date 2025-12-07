using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    public class Bonificacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Id_Planilla { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        [Required]
        [MaxLength(255)]
        public string Motivo { get; set; } = null!;

        [Required]
        public DateTime Fecha { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [ForeignKey("Id_Planilla")]
        public virtual Planilla Planilla { get; set; } = null!;
    }
}
