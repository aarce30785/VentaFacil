using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models.Dto
{
    public class PedidoItemDto
    {
        public int Id_Detalle { get; set; }
        public int Id_Producto { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }

        public string Notas { get; set; } = string.Empty;
        public decimal Subtotal => PrecioUnitario * Cantidad;
    }
}
