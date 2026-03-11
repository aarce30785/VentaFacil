using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    public class Nomina
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id_Nomina { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFinal { get; set; }

        public DateTime FechaGeneracion { get; set; }

        public string Estado { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalBruto { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalDeducciones { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalNeto { get; set; }

        public virtual ICollection<Planilla> Planillas { get; set; }
    }
}