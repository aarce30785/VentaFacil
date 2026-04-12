using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Admin
{
    public class ListCategoriaResponse
    {
        public List<CategoriaDto> Categorias { get; set; } = new();
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public string Busqueda { get; set; }
        public int PaginaActual { get; set; }
        public int CantidadPorPagina { get; set; } = 10;
        public int TotalCategorias { get; set; }

        public int TotalPaginas
        {
            get => (int)Math.Ceiling((double)TotalCategorias / CantidadPorPagina);
            set { }
        }

        public string AccionModal { get; set; }
        public CategoriaDto CategoriaSeleccionada { get; set; }
    }
}
