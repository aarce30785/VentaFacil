using System.Collections.Generic;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Pedido
{
    public class ListPedidoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<PedidoDto> Pedidos { get; set; } = new();
    }
}
