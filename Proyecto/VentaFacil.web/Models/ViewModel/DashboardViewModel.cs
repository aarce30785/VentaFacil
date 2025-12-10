using System;
using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.ViewModel
{
    public class DashboardViewModel
    {
        // Ventas totales
        public decimal VentasDia { get; set; }
        public decimal VentasSemana { get; set; }
        public decimal VentasMes { get; set; }

        // Productos más vendidos
        public List<ProductoMasVendidoDto> ProductosMasVendidos { get; set; } = new();

        // Notificaciones de stock mínimo
        public List<InventarioDto> StockMinimo { get; set; } = new();

        // Últimas órdenes realizadas
        public List<PedidoDto> UltimasOrdenes { get; set; } = new();

        // Ingresos y gastos recientes
        public decimal IngresosRecientes { get; set; }
        public decimal GastosRecientes { get; set; }
    }

    public class ProductoMasVendidoDto
    {
        public int Id_Producto { get; set; }
        public string Nombre { get; set; }
        public int CantidadVendida { get; set; }
    }
}
