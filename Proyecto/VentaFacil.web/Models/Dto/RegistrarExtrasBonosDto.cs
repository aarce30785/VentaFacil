namespace VentaFacil.web.Models.Dto
{
    public class RegistrarExtrasBonosDto
    {
        public int Id_Planilla { get; set; }

        public decimal HorasExtras { get; set; } = 0;

        public decimal MontoBonificaciones { get; set; } = 0;

        public string? Observaciones { get; set; }
    }
}
