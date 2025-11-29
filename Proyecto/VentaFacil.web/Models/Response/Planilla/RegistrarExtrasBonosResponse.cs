namespace VentaFacil.web.Models.Response.Planilla
{
    public class RegistrarExtrasBonosResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Id_Planilla { get; set; }
        public decimal HorasExtras { get; set; }
        public decimal Bonificaciones { get; set; }
    }
}