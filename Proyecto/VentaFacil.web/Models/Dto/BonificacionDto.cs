using System;

namespace VentaFacil.web.Models.Dto
{
    public class BonificacionDto
    {
        public int Id_Planilla { get; set; }
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = null!;
        public DateTime Fecha { get; set; }
    }
}
