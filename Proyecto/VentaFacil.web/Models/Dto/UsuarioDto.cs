using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class UsuarioDto
    {
        public int Id_Usr { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string Correo { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Contrasena { get; set; }

        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarContrasena { get; set; }

        public DateTime? FechaCreacion { get; set; }

        [Required(ErrorMessage = "El rol es requerido")]
        [Range(1, 4, ErrorMessage = "El rol no es válido")]
        public int Rol { get; set; }

        public bool Estado { get; set; } = true;
    }
}
