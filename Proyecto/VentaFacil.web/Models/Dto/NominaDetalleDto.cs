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
        public string? Observaciones { get; set; }

        public decimal TotalBruto { get; set; }
        public decimal TotalDeducciones { get; set; }
        public decimal TotalNeto { get; set; }

        /// <summary>Deducciones activas usadas para calcular la nómina (nombre + %).</summary>
        public List<DeduccionResumenDto> DeduccionesAplicadas { get; set; } = new();

        public List<PlanillaDetalleItemDto> Detalles { get; set; } = new();
    }

    /// <summary>Resumen de una deducción de ley con su porcentaje.</summary>
    public class DeduccionResumenDto
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; }
    }

    public class PlanillaDetalleItemDto
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public decimal HorasTrabajadas { get; set; }
        public decimal HorasExtras { get; set; }
        public decimal Bonificaciones { get; set; }
        public decimal SalarioBruto { get; set; }
        public decimal Deducciones { get; set; }
        public decimal SalarioNeto { get; set; }

        /// <summary>Nota explicativa cuando se omite alguna deducción (ej: bruto ₡0).</summary>
        public string? Observaciones { get; set; }

        /// <summary>Desglose individual de cada deducción para este empleado.</summary>
        public List<DeduccionDetalleItemDto> DeduccionesDetalle { get; set; } = new();

        /// <summary>Detalle de cada jornada trabajada dentro del período de esta nómina.</summary>
        public List<PlanillaDiaDto> DiasLaborados { get; set; } = new();
    }

    /// <summary>Monto de una deducción específica para un empleado concreto.</summary>
    public class DeduccionDetalleItemDto
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; }
        public decimal Monto { get; set; }
    }
}
