using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("InventarioMovimientoAuditoria")]
    public class InventarioMovimientoAuditoria
    {
        [Key]
        public int Id_Auditoria { get; set; }

        public int Id_Movimiento { get; set; }
        
        public int Id_Inventario { get; set; }
        
        public int CantidadAnterior { get; set; }
        
        public int CantidadNueva { get; set; }
        
        public string? TipoMovimientoAnterior { get; set; }
        
        public string? TipoMovimientoNuevo { get; set; }
        
        public string? MotivoCambio { get; set; }
        
        public DateTime FechaCambio { get; set; } = DateTime.Now;
        
        public int Id_UsuarioResponsable { get; set; }

        [ForeignKey("Id_Movimiento")]
        public virtual InventarioMovimiento? Movimiento { get; set; }

        [ForeignKey("Id_Inventario")]
        public virtual Inventario? Inventario { get; set; }

        [ForeignKey("Id_UsuarioResponsable")]
        public virtual Usuario? UsuarioResponsable { get; set; }
    }
}
