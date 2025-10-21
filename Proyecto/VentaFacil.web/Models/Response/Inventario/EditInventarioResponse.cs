namespace VentaFacil.web.Models.Response.Inventario
{
    public class EditInventarioResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int InventarioId { get; set; }
    }
}
