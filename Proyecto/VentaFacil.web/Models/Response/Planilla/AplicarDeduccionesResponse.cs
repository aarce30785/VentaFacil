namespace VentaFacil.web.Models.Response.Planilla
{
    public class AplicarDeduccionesResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Id_Nomina { get; set; }
        public int RegistrosAfectados { get; set; }
        public decimal TotalDeduccionesAplicadas { get; set; }
    }
}