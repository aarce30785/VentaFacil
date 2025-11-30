using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace VentaFacil.web.Models.ViewModel
{
    public class RegistrarEntradaViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un insumo")]
        [Display(Name = "Insumo")]
        public int IdInventario { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [Display(Name = "Cantidad a Ingresar")]
        public int Cantidad { get; set; }

        [Display(Name = "Observaciones")]
        [StringLength(200, ErrorMessage = "La observación no puede exceder los 200 caracteres")]
        public string? Observaciones { get; set; }

        // Para la lista desplegable
        public IEnumerable<SelectListItem>? Inventarios { get; set; }
    }
}
