using System;

using System;

namespace VentaFacil.web.Models.Dto
{
    public class NominaConsultaDto
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }
        public string? Estado { get; set; }
        public int? Id_Usr { get; set; }
        public string? TipoPeriodo { get; set; }
        public string? BusquedaUsuario { get; set; }
        public int Pagina { get; set; }
        public int CantidadPorPagina { get; set; }
    }
}