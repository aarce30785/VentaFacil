using System;

namespace VentaFacil.web.Models.Dto
{
    public class GenerarNominaDto
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinal { get; set; }
        public bool IncluirSoloUsuariosActivos { get; set; }
        public string? Comentarios { get; set; }
    }
}