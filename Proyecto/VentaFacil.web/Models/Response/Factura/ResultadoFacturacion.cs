using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Factura
{
    public class ResultadoFacturacion
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int FacturaId { get; set; }
        public FacturaDto Factura { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public static ResultadoFacturacion Exitoso(FacturaDto factura, string mensaje = "", List<string> warnings = null)
        {
            return new ResultadoFacturacion
            {
                Success = true,
                Message = mensaje,
                FacturaId = factura?.Id_Factura ?? 0,
                Factura = factura,
                Warnings = warnings ?? new List<string>()
            };
        }

        public static ResultadoFacturacion Error(string mensaje, List<string> errores = null)
        {
            return new ResultadoFacturacion
            {
                Success = false,
                Message = mensaje,
                Errors = errores ?? new List<string>(),
                FacturaId = 0
            };
        }
    }
}
