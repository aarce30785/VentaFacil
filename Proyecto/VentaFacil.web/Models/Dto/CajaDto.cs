using System;
using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class CajaDto
    {
        [Key]
        [Display(Name = "ID Caja")]
        public int Id_Caja { get; set; }

        [Display(Name = "ID Usuario")]
        public int Id_Usuario { get; set; }

        [Display(Name = "Fecha Apertura")]
        public DateTime Fecha_Apertura { get; set; }

        [Display(Name = "Fecha Cierre")]
        public DateTime? Fecha_Cierre { get; set; }

        [Display(Name = "Monto Inicial")]
        public decimal Monto_Inicial { get; set; }

        [Display(Name = "Monto")]
        public decimal? Monto { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; }
    }
}
