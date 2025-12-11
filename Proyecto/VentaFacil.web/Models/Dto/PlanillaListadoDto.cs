namespace VentaFacil.web.Models.Dto
{
    /// <summary>
    /// DTO sencillo para mostrar planillas en un dropdown
    /// cuando se registran extras/bonificaciones.
    /// </summary>
    public class PlanillaListadoDto
    {
        public int Id_Planilla { get; set; }
        public int Id_Usr { get; set; }
        public string NombreUsuario { get; set; }

        public string EstadoRegistro { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinal { get; set; }

        // Esto se mostrará en el combo "Planilla (Periodo)"
        public string Periodo =>
            $"{FechaInicio:dd/MM/yyyy} - {(FechaFinal.HasValue ? FechaFinal.Value.ToString("dd/MM/yyyy") : "Pendiente")}";

        public string InfoCompleta => $"{NombreUsuario} | {Periodo}";
    }
}
