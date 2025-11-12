using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.Dto
{
    public class UsuarioPerfilDto : IValidatableObject
    {
        public int Id_Usr { get; set; }

        
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        public string Correo { get; set; }

        [Display(Name = "Contraseña Actual")]
        public string ContrasenaActual { get; set; }

        [Display(Name = "Nueva Contraseña")]
        public string NuevaContrasena { get; set; }

        [Display(Name = "Confirmar Nueva Contraseña")]
        public string ConfirmarContrasena { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            
            if (!string.IsNullOrWhiteSpace(NuevaContrasena))
            {
                if (string.IsNullOrWhiteSpace(ContrasenaActual))
                    yield return new ValidationResult("La contraseña actual es requerida para cambiar la contraseña",
                        new[] { nameof(ContrasenaActual) });

                if (NuevaContrasena.Length < 6)
                    yield return new ValidationResult("La nueva contraseña debe tener al menos 6 caracteres",
                        new[] { nameof(NuevaContrasena) });

                if (NuevaContrasena != ConfirmarContrasena)
                    yield return new ValidationResult("Las contraseñas no coinciden",
                        new[] { nameof(ConfirmarContrasena) });
            }

            if (string.IsNullOrWhiteSpace(Nombre) &&
                string.IsNullOrWhiteSpace(Correo) &&
                string.IsNullOrWhiteSpace(NuevaContrasena))
            {
                yield return new ValidationResult("Debe proporcionar al menos un campo para actualizar (Nombre, Correo o Contraseña)");
            }
        }
    }
}
