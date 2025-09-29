namespace VentaFacil.web.Models.Response
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public int UsuarioId { get; set; }
    }
}
