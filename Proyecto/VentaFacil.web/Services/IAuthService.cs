using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response;

namespace VentaFacil.web.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginDto loginDto);
    }
}
