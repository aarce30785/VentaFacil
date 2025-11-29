using System;

namespace VentaFacil.web.Models.Response.Planilla
{
    public class RegistrarHorasResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? Id_Planilla { get; set; }
        public decimal? HorasTrabajadas { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }
    }
}