using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Enums;

namespace VentaFacil.web.Models.Dto
{
    public class PedidoDto
    {
        public int Id_Venta { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public PedidoEstado Estado { get; set; } = PedidoEstado.Borrador;
        public int Id_Usuario { get; set; }
        public string? Cliente { get; set; }
        public ModalidadPedido Modalidad { get; set; }
        public int? NumeroMesa { get; set; }
        public string? MotivoCancelacion { get; set; }
        public List<PedidoItemDto> Items { get; set; } = new List<PedidoItemDto>();

        // Método para validar si el pedido puede ser guardado
        public bool PuedeSerGuardado()
        {
            return Items.Any() &&
                   (Modalidad != ModalidadPedido.EnMesa || NumeroMesa.HasValue);
        }
    }
}