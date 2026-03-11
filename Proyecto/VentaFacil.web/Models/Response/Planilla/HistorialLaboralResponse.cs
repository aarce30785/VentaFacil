using System.Collections.Generic;

namespace VentaFacil.web.Models.Response.Planilla
{
    public class HistorialLaboralResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        public int Id_Usr { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;

        public List<Models.Dto.PlanillaDiaDto> Jornadas { get; set; } = new();

        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int CantidadPorPagina { get; set; }
    }
}
