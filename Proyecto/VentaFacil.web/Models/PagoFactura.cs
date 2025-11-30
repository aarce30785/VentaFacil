using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("PagoFactura")]
    public class PagoFactura
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int FacturaId { get; set; }

        [Required]
        [StringLength(20)]
        public string MetodoPago { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        [StringLength(3)]
        public string Moneda { get; set; } = "CRC";

        [Column(TypeName = "decimal(10,4)")]
        public decimal? TasaCambio { get; set; }

        [ForeignKey("FacturaId")]
        public virtual Factura Factura { get; set; }
    }
}
