using System;

namespace VentaFacil.web.Models.Dto
{
    public class RegistrarHorasDto
    {
        public int? Id_Planilla { get; set; }
        public int Id_Usr { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinal { get; set; }
        public int HorasTrabajadas { get; set; }   // ← AQUÍ EL CAMBIO
        public string? Observaciones { get; set; }
    }
}
