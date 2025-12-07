using System;
using System.Collections.Generic;

namespace VentaFacil.web.Models.Dto
{
    public class NominaDetalleDto
    {
        public int Id_Nomina { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFinal { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public string Estado { get; set; } = string.Empty;
        
        public decimal TotalBruto { get; set; }
        public decimal TotalDeducciones { get; set; }
        public decimal TotalNeto { get; set; }

        public List<PlanillaDetalleItemDto> Detalles { get; set; } = new List<PlanillaDetalleItemDto>();
    }

    public class PlanillaDetalleItemDto
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty; // Opcional si existe en Usuario
        public int HorasTrabajadas { get; set; }
        public decimal HorasExtras { get; set; }
        public decimal Bonificaciones { get; set; }
        public decimal SalarioBruto { get; set; }
        public decimal Deducciones { get; set; }
        public decimal SalarioNeto { get; set; }
    }
}
