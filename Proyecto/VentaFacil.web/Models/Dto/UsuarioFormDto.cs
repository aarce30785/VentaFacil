using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class UsuarioFormDto
    {
        public int Id_Usr { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string Correo { get; set; }

        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Contrasena { get; set; }

        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarContrasena { get; set; }

        [Required(ErrorMessage = "El rol es requerido")]
        public string Rol { get; set; }

        public bool Estado { get; set; }
    }
}
