using System;
using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Factura
{
    public class ListFacturaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<FacturaDto> Facturas { get; set; } = new();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int CantidadPorPagina { get; set; }
        public int TotalRegistros { get; set; }
        public decimal TotalMonto { get; set; }
        public int TotalAnuladas { get; set; }
        
        // Filtros persistidos
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? NumeroFactura { get; set; }
        public string? Cliente { get; set; }
    }
}
