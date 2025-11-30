using System.Collections.Generic;
using VentaFacil.web.Models;

namespace VentaFacil.web.Models.Response.Planilla
{
    public class NominaConsultaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<Nomina>? Nominas { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
    }
}