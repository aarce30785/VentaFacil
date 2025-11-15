using System;
using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class CajaRetiroDto
    {
        [Display(Name = "ID Retiro")]
        public int Id { get; set; }

        [Display(Name = "ID Caja")]
        public int Id_Caja { get; set; }

        [Display(Name = "ID Usuario")]
        public int Id_Usuario { get; set; }

        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Display(Name = "Motivo")]
        public string Motivo { get; set; } = string.Empty;

        [Display(Name = "Fecha y Hora")]
        public DateTime FechaHora { get; set; }
    }
}
