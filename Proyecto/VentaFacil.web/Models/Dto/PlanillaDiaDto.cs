using System;

namespace VentaFacil.web.Models.Dto
{
    /// <summary>Represents a single worked day/shift entry in the employee's labor history.</summary>
    public class PlanillaDiaDto
    {
        public int Id_Planilla { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }
        public DateTime? HoraInicioPausa { get; set; }
        public DateTime? HoraFinPausa { get; set; }
        public decimal HorasTrabajadas { get; set; }
        public decimal HorasExtras { get; set; }
        public decimal SalarioBruto { get; set; }
        public string EstadoRegistro { get; set; } = string.Empty;
        public int? Id_Nomina { get; set; }
    }
}
