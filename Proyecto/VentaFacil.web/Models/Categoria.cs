using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    [Table("Categoria")]
    public class Categoria
    {
        [Key]
        [Column("Id_Categoria")]
        public int Id_Categoria { get; set; }

        [Required]
        [MaxLength(255)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string? Descripcion { get; set; }
    }
}
