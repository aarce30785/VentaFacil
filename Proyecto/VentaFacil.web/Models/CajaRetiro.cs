using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    public class CajaRetiro
    {
        [Key]
        [Column("Id_Retiro")] 
        public int Id_Retiro { get; set; }

        [Required]
        public int Id_Caja { get; set; }

        [Required]
        public int Id_Usuario { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        [MaxLength(255)]
        public string Motivo { get; set; } = string.Empty;

        [Required]
        public DateTime FechaHora { get; set; }
    }
}
