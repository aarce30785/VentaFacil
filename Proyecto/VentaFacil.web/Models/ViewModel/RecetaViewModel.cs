using Microsoft.AspNetCore.Mvc.Rendering;

namespace VentaFacil.web.Models.ViewModel
{
    public class RecetaViewModel
    {
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public List<ProductoInsumoViewModel> Insumos { get; set; } = new List<ProductoInsumoViewModel>();
        public List<SelectListItem> InventarioDisponible { get; set; } = new List<SelectListItem>();
    }

    public class ProductoInsumoViewModel
    {
        public int Id_ProductoInsumo { get; set; }
        public int Id_Inventario { get; set; }
        public string NombreInsumo { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}
