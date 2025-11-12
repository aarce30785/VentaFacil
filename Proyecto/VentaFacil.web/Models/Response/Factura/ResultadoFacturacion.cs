using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Factura
{
    public class ResultadoFacturacion
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public FacturaDto Factura { get; set; }
        public List<string> Errores { get; set; } = new List<string>();

        public static ResultadoFacturacion Exitoso(FacturaDto factura, string mensaje = "Factura generada exitosamente")
        {
            return new ResultadoFacturacion
            {
                Success = true,
                Message = mensaje,
                Factura = factura
            };
        }

        public static ResultadoFacturacion Error(string mensaje, List<string> errores = null)
        {
            return new ResultadoFacturacion
            {
                Success = false,
                Message = mensaje,
                Errores = errores ?? new List<string>()
            };
        }
    }
}
