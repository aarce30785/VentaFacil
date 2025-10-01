namespace VentaFacil.web.Models.Response.Producto
{
    public class EditProductoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProductoId { get; set; }
    }
}