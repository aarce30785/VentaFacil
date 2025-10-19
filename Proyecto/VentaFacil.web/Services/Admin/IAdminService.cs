
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Admin;

namespace VentaFacil.web.Services.Admin
{
    public interface IAdminService
    {
        Task<bool> ActualizarUsuarioAsync(UsuarioDto model);
        Task<bool> CrearUsuarioAsync(UsuarioDto model);
        Task<bool> EliminarUsuarioAsync(int id);
        Task<RolResponse> GetRolByIdAsync(int id);
        Task<UsuarioResponse> GetUsuarioByIdAsync(int id);
        Task<UsuarioListResponse> GetUsuariosPaginadosAsync(int pagina = 1, int cantidadPorPagina = 10);

    }
}
