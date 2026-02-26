using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models
{
    /// <summary>
    /// Tabla de una sola fila que almacena las preferencias globales de notificación.
    /// </summary>
    public class ConfiguracionNotificacion
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Si es true, el sistema envía emails cuando se detecta stock bajo.</summary>
        public bool AlertaStockEmail { get; set; } = false;

        /// <summary>Dirección de correo destino para las alertas. Solo se usa cuando AlertaStockEmail = true.</summary>
        [MaxLength(255)]
        public string? CorreoDestino { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
