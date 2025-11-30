using System.ComponentModel.DataAnnotations;
using VentaFacil.web.Models.Enum;

namespace VentaFacil.web.Models.Response.Factura
{
    public class ProcesarPagoResponse
    {
        [Required(ErrorMessage = "El ID del pedido es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del pedido debe ser válido")]
        public int PedidoId { get; set; }

        [Required(ErrorMessage = "El método de pago es requerido")]
        public MetodoPago MetodoPago { get; set; }

        [Required(ErrorMessage = "El monto pagado es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal MontoPagado { get; set; }

        [Required(ErrorMessage = "La moneda es requerida")]
        [RegularExpression("^(CRC|USD)$", ErrorMessage = "La moneda debe ser CRC o USD")]
        public string Moneda { get; set; } = "CRC";

        // Solo requerido cuando la moneda es USD
        [Range(0.01, double.MaxValue, ErrorMessage = "La tasa de cambio debe ser mayor a 0")]
        public decimal? TasaCambio { get; set; }

        // Propiedades de validación personalizada
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validar que si la moneda es USD, la tasa de cambio es requerida
            if (Moneda == "USD" && (!TasaCambio.HasValue || TasaCambio.Value <= 0))
            {
                results.Add(new ValidationResult(
                    "La tasa de cambio es requerida para pagos en dólares",
                    new[] { nameof(TasaCambio) }
                ));
            }

            // Validar que si la moneda es CRC, no se envíe tasa de cambio
            if (Moneda == "CRC" && TasaCambio.HasValue)
            {
                results.Add(new ValidationResult(
                    "La tasa de cambio no debe ser especificada para pagos en colones",
                    new[] { nameof(TasaCambio) }
                ));
            }

            return results;
        }
    }
}
