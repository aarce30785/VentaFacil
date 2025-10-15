using VentaFacil.web.Models.Response.Usuario;

namespace VentaFacil.web.Models.Response.Admin
{
    public class UsuarioListResponse
    {
        public List<UsuarioResponse> Usuarios { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalUsuarios { get; set; }
        public UsuarioResponse UsuarioSeleccionado { get; set; }
        public string AccionModal { get; set; }
        public string? Busqueda { get; set; }
        public int? RolFiltro { get; set; }
    }
}
