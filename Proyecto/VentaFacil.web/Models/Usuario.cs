using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    public class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id_Usr { get; set; }
        [Required]
        [MaxLength(255)]
        public string Nombre { get; set; } = null!;
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Correo { get; set; } = null!;
        [Required]
        public string Contrasena { get; set; } = null!;
        public bool Estado { get; set; } = true;
        public DateTime FechaCreacion { get; set; }

        [ForeignKey("Rol")]
        public int Rol { get; set; }

        public Rol? RolNavigation { get; set; }
    }
}
