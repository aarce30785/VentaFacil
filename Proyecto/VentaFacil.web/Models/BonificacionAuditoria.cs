using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    public class BonificacionAuditoria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Id_Bonificacion { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoAnterior { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoNuevo { get; set; }

        [MaxLength(1024)]
        public string? MotivoCambio { get; set; }

        public DateTime FechaCambio { get; set; } = DateTime.Now;

        [Required]
        public int Id_UsuarioResponsable { get; set; }

        [ForeignKey("Id_Bonificacion")]
        public virtual Bonificacion Bonificacion { get; set; } = null!;

        [ForeignKey("Id_UsuarioResponsable")]
        public virtual Usuario UsuarioResponsable { get; set; } = null!;
    }
}
