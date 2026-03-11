using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response;

namespace VentaFacil.web.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<LoginResponse> LoginAsync(LoginDto loginDto)
        {
            var response = new LoginResponse();

            try
            {
                var usuario = await _context.Usuario.Include(u => u.RolNavigation)
                    .FirstOrDefaultAsync(u => u.Correo == loginDto.Correo && u.Estado);
                
                if (usuario == null)
                {
                    response.Success = false;
                    response.Message = "usuario no encontrado o inactivo";
                    return response;
                }

                var hasher = new PasswordHasher<string>();
                var result  = hasher.VerifyHashedPassword(null, usuario.Contrasena, loginDto.Contrasena);

                if (result == PasswordVerificationResult.Failed)
                {
                    response.Success = false;
                    response.Message = "contraseña incorrecta";
                    return response;
                }

                response.Success = true;
                response.Message = "login exitoso";
                response.Nombre = usuario.Nombre;
                response.Rol = usuario.RolNavigation?.Nombre_Rol;
                response.UsuarioId = usuario.Id_Usr;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error durante el login: {ex.Message}";
                return response;
            }

            return response;
        }
    }
}
