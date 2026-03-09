using System;

namespace VentaFacil.web.Models.Dto
{
    public class GenerarNominaDto
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinal { get; set; }
        public bool IncluirSoloUsuariosActivos { get; set; } = true;
        public string? Comentarios { get; set; }
        /// <summary>Semanal | Trimestral | Personalizado</summary>
        public string TipoPeriodo { get; set; } = "Semanal";
    }
}