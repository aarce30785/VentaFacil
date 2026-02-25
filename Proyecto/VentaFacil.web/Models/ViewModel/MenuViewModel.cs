using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.ViewModel
{
    public class MenuViewModel
    {
        public List<ProductoDto> Productos { get; set; } = new List<ProductoDto>();
        public List<CategoriaDto> Categorias { get; set; } = new List<CategoriaDto>();
        public int? CategoriaSeleccionadaId { get; set; }
        public string? Busqueda { get; set; }
    }
}
