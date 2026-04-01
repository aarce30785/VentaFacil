using System;
using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Caja
{
    public class ListCajaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CajaDto> Cajas { get; set; } = new();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int CantidadPorPagina { get; set; }
        public int TotalRegistros { get; set; }
    }
}
