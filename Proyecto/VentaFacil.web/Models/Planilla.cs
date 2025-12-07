using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    public class Planilla
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id_Planilla { get; set; }

        [ForeignKey("Usuario")]
        public int Id_Usr { get; set; }
        public virtual Usuario Usuario { get; set; }

        [ForeignKey("Nomina")]
        public int? Id_Nomina { get; set; }
        public virtual Nomina? Nomina { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFinal { get; set; }

        public int HorasTrabajadas { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal HorasExtras { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Bonificaciones { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Deducciones { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal SalarioBruto { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal SalarioNeto { get; set; }

        public string EstadoRegistro { get; set; } = string.Empty;
    }
}