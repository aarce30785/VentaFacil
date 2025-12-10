using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
        public string Contrasena { get; set; }

        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarContrasena { get; set; }
    }
}
