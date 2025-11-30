using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VentaFacil.web.Models.Enum;
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
        public int? FacturaId { get; set; }

        public string? NumeroFactura { get; set; }

        public decimal Descuento { get; set; } = 0m;
        public string Notas { get; set; } = string.Empty;
        public DateTime? FechaActualizacion { get; set; }
        public string? UsuarioActualizacion { get; set; }
        public TimeSpan? TiempoPreparacion
        {
            get
            {
                if (FechaActualizacion.HasValue && Estado >= PedidoEstado.EnPreparacion)
                {
                    return FechaActualizacion.Value - Fecha;
                }
                return null;
            }
        }

        public List<EstadoPedidoDto> HistorialEstados { get; internal set; }

        // Método para validar si el pedido puede ser guardado
        public bool PuedeSerGuardado()
        {
            return Items.Any() &&
                   (Modalidad != ModalidadPedido.EnMesa || NumeroMesa.HasValue);
        }

        // MÉTODO AGREGADO: Verificar si tiene factura
        public bool TieneFactura()
        {
            return FacturaId.HasValue && FacturaId.Value > 0;
        }

        // NUEVO MÉTODO: Obtener el nombre de la factura para mostrar
        public string ObtenerNombreFactura()
        {
            if (!TieneFactura() || string.IsNullOrEmpty(NumeroFactura))
                return "Sin factura";

            return NumeroFactura;
        }

        public string ObtenerIconoModalidad()
        {
            return Modalidad == ModalidadPedido.EnMesa ? "lni lni-restaurant" : "lni lni-takeaway";
        }

        public string ObtenerTextoModalidad()
        {
            return Modalidad == ModalidadPedido.EnMesa ? $"Mesa {NumeroMesa}" : "Para Llevar";
        }

        public string ObtenerClaseBadgeEstado()
        {
            return Estado switch
            {
                PedidoEstado.Borrador => "secondary-btn",
                PedidoEstado.EnPreparacion => "warning-btn",
                PedidoEstado.Listo => "success-btn",
                PedidoEstado.Entregado => "info-btn",
                PedidoEstado.Cancelado => "danger-btn",
                _ => "secondary-btn"
            };
        }

        public string ObtenerTextoEstado()
        {
            return Estado switch
            {
                PedidoEstado.Borrador => "Borrador",
                PedidoEstado.EnPreparacion => "En Cocina",
                PedidoEstado.Listo => "Listo",
                PedidoEstado.Entregado => "Entregado",
                PedidoEstado.Cancelado => "Cancelado",
                _ => Estado.ToString()
            };
        }

        // MÉTODOS DE TRANSICIÓN DE ESTADO
        public bool PuedeEnviarACocina()
        {
            return Estado == PedidoEstado.Borrador && Items.Any();
        }

        public bool PuedeMarcarListo()
        {
            return Estado == PedidoEstado.EnPreparacion;
        }

        public bool PuedeMarcarEntregado()
        {
            return Estado == PedidoEstado.Listo;
        }

        public bool PuedeCancelar()
        {
            return Estado == PedidoEstado.Borrador || Estado == PedidoEstado.EnPreparacion;
        }
    }
}