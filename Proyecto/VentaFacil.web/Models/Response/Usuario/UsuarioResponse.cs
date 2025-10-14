using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Models.Response.Usuario
{
    public class UsuarioResponse
    {
        public int Id_Usr { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public bool? Estado { get; set; }
        public int? RolId { get; set; }
        public string Rol { get; set; }

        public static implicit operator UsuarioResponse(UsuarioFormDto v)
        {
            throw new NotImplementedException();
        }
    }
}
