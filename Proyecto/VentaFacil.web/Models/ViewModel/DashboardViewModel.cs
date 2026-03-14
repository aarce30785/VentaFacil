using System;
using System.Collections.Generic;
using VentaFacil.web.Models.Dto;
namespace VentaFacil.web.Models.ViewModel
{
    public class DashboardViewModel
    {
        public bool MostrarAlertaCajaAbierta { get; set; }
        public decimal VentasDia { get; set; }
        public decimal VentasSemana { get; set; }
        public decimal VentasMes { get; set; }
        public List<ProductoMasVendidoDto> ProductosMasVendidos { get; set; } = new List<ProductoMasVendidoDto>();
        public List<InventarioDto> StockMinimo { get; set; } = new List<InventarioDto>();
        public List<PedidoDto> UltimasOrdenes { get; set; } = new List<PedidoDto>();
        public decimal IngresosRecientes { get; set; }
        public decimal GastosRecientes { get; set; }
    }
    public class ProductoMasVendidoDto
    {
        public int Id_Producto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
    }
}
