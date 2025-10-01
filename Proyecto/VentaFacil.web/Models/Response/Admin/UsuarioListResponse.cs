namespace VentaFacil.web.Models.Response.Admin
{
    public class UsuarioListResponse
    {
        public List<UsuarioResponse> Usuarios { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalUsuarios { get; set; }
    }
}
