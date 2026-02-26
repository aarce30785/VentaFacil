using System;
using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class InventarioMovimientoDto
    {
        [Key]
        [Display(Name = "ID Movimiento")]
        public int Id_Movimiento { get; set; }

        [Display(Name = "ID Inventario")]
        public int Id_Inventario { get; set; }

        [Display(Name = "Tipo de Movimiento")]
        public string? Tipo_Movimiento { get; set; }

        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }

        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; }

        [Display(Name = "ID Usuario")]
        public int Id_Usuario { get; set; }

        [Display(Name = "Usuario")]
        public string? Nombre_Usuario { get; set; }
    }
}
