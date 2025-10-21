using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Usuario;

namespace VentaFacil.web.Models.ViewModel
{
    public class PerfilViewModel
    {
        public UsuarioResponse Usuario { get; set; }
        public UsuarioPerfilDto Edicion { get; set; }
    }
}
