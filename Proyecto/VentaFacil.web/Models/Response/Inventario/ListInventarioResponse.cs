using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Inventario
{
    public class ListInventarioResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<InventarioDto> Inventarios { get; set; } = new();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int CantidadPorPagina { get; set; }
        public int TotalRegistros { get; set; }
        public string? Busqueda { get; set; }

        // Totales para las stats cards
        public int TotalInsumos { get; set; }
        public int StockBajo { get; set; }
    }
}
