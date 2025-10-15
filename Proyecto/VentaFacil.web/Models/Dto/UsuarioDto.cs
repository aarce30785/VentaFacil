using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class UsuarioDto : IValidatableObject
    {
        public int Id_Usr { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string Correo { get; set; }

        // REMOVER DataAnnotations de Contrasena y ConfirmarContrasena
        public string Contrasena { get; set; }

        public string ConfirmarContrasena { get; set; }

        public DateTime? FechaCreacion { get; set; }

        [Required(ErrorMessage = "El rol es requerido")]
        [Range(1, 4, ErrorMessage = "El rol no es válido")]
        public int Rol { get; set; }

        public bool Estado { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validación para creación
            if (Id_Usr == 0)
            {
                if (string.IsNullOrWhiteSpace(Contrasena))
                {
                    yield return new ValidationResult(
                        "La contraseña es requerida para crear un usuario",
                        new[] { nameof(Contrasena) }
                    );
                }
                else if (Contrasena.Length < 6)
                {
                    yield return new ValidationResult(
                        "La contraseña debe tener al menos 6 caracteres",
                        new[] { nameof(Contrasena) }
                    );
                }
            }

            // Validación para edición (solo si se proporciona contraseña)
            if (Id_Usr > 0 && !string.IsNullOrWhiteSpace(Contrasena))
            {
                if (Contrasena.Length < 6)
                {
                    yield return new ValidationResult(
                        "La contraseña debe tener al menos 6 caracteres",
                        new[] { nameof(Contrasena) }
                    );
                }
            }

            // Validación de confirmación de contraseña
            if (!string.IsNullOrWhiteSpace(Contrasena) && Contrasena != ConfirmarContrasena)
            {
                yield return new ValidationResult(
                    "Las contraseñas no coinciden",
                    new[] { nameof(ConfirmarContrasena) }
                );
            }
        }
    }
}
