using Microsoft.AspNetCore.Mvc.Rendering;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Admin;
using VentaFacil.web.Models.Response.Usuario;

namespace VentaFacil.web.Services.Usuario
{
    public interface IUsuarioService
    {
        Task<RolResponse> GetRolByIdAsync(int id);
        Task<UsuarioDto> GetUsuarioByIdAsync(int id);
        Task<IEnumerable<SelectListItem>> GetRolesAsync();
    }
}
