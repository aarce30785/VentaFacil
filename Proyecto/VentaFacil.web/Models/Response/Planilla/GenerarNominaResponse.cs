namespace VentaFacil.web.Models.Response.Planilla
{
    public class GenerarNominaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Id_Nomina { get; set; }
        public decimal TotalBruto { get; set; }
        public decimal TotalDeducciones { get; set; }
        public decimal TotalNeto { get; set; }
    }
}