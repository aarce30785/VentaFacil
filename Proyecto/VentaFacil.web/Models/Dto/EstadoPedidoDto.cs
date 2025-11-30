using VentaFacil.web.Models.Enum;

namespace VentaFacil.web.Models.Dto
{
    public class EstadoPedidoDto
    {
        public PedidoEstado Estado { get; set; }
        public DateTime Fecha { get; set; }
        public string Observacion { get; set; }
    }
}