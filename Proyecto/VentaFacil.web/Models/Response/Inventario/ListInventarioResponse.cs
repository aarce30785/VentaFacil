using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Inventario
{
    public class ListInventarioResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<InventarioDto> Inventarios { get; set; } = new();
    }
}
