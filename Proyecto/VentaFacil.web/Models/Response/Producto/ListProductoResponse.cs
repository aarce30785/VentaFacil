using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Producto
{
    public class ListProductoResponse
    {
        public List<ProductoDto> Productos { get; set; } = new();
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public string Busqueda { get; set; }
        public int? CategoriaFiltro { get; set; }
        public bool MostrarInactivos { get; set; }
        public int PaginaActual { get; set; }
        public int CantidadPorPagina { get; set; } = 10;
        public int TotalProductos { get; set; }

        
        public int TotalPaginas
        {
            get => (int)Math.Ceiling((double)TotalProductos / CantidadPorPagina);
            set { }
        }

        public List<SelectListItem> Categorias { get; set; }

        public string AccionModal { get; set; }
        public ProductoDto ProductoSeleccionado { get; set; }
    }
}