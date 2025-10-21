using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Inventario
{
    public class GetInventarioResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public InventarioDto? Inventario { get; set; }
    }
}
