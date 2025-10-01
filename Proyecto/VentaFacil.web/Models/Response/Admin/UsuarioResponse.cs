namespace VentaFacil.web.Models.Response.Admin
{
    public class UsuarioResponse
    {
        public int Id_Usr { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public bool? Estado { get; set; }
        public int? RolId { get; set; }
        public string Rol { get; set; }
    }
}
