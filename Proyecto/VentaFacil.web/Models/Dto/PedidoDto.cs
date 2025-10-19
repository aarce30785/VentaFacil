using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models.Dto
{
    public class PedidoDto
    {
        [Key]
        [Display(Name = "ID Pedido")]
        public int Id_Venta { get; set; }

        [Display(Name = "Cliente")]
        public string? Cliente { get; set; }

        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Display(Name = "Total")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Display(Name = "Estado")]
        public bool Estado { get; set; } = false; // false = pendiente/borrador, true = finalizado

        [Display(Name = "Usuario")]
        public int Id_Usuario { get; set; }

        [Display(Name = "Productos del Pedido")]
        public List<PedidoItemDto> Items { get; set; } = new();
    }
}
