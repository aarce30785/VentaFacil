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

                if (!string.IsNullOrEmpty(usuario.SessionToken) && !loginDto.ForzarReemplazo)
                {
                    response.Success = false;
                    response.RequiereConfirmacion = true;
                    response.Message = "Existe una sesión activa en otro dispositivo. ¿Desea cerrarla y continuar?";
                    return response;
                }

                usuario.SessionToken = Guid.NewGuid().ToString();
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "login exitoso";
                response.Nombre = usuario.Nombre;
                response.Rol = usuario.RolNavigation?.Nombre_Rol;
                response.UsuarioId = usuario.Id_Usr;
                response.SessionToken = usuario.SessionToken;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error durante el login: {ex.Message}";
                return response;
            }

            return response;
        }

        public async Task LogoutAsync(int usuarioId)
        {
            var usuario = await _context.Usuario.FindAsync(usuarioId);
            if (usuario != null)
            {
                usuario.SessionToken = null;
                await _context.SaveChangesAsync();
            }
        }
    }
}
