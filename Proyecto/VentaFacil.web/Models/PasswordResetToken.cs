using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VentaFacil.web.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        public bool IsUsed { get; set; } = false;
    }
}
