namespace VentaFacil.web.Models.Response.Inventario
{
    public class RegisterInventarioResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int InventarioId { get; set; }
    }
}
