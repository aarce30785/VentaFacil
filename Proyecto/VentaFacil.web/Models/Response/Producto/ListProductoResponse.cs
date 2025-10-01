using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Producto
{
    public class ListProductoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ProductoDto> Productos { get; set; } = new();
    }
}