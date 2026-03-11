using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Helpers;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Admin;
using VentaFacil.web.Models.Response.Usuario;

namespace VentaFacil.web.Services.Usuario
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _context;

        public UsuarioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RolResponse> GetRolByIdAsync(int id)
        {
            var rol = await _context.Rol.FindAsync(id);

            if (rol == null)
            {
                throw new Exception("Rol no encontrado");
            }
            return new RolResponse
            {
                Id_Rol = rol.Id_Rol,
                Nombre = rol.Nombre_Rol,
                Descripcion = rol.Descripcion
            };
        }

        public async Task<IEnumerable<SelectListItem>> GetRolesAsync()
        {
            try
            {
               
                var roles = await _context.Rol
                    
                    .Select(r => new SelectListItem
                    {
                        Value = r.Id_Rol.ToString(),
                        Text = r.Nombre_Rol
                    })
                    .ToListAsync();

                return roles ?? new List<SelectListItem>();
            }
            catch (Exception ex)
            {
                return new List<SelectListItem>();
            }
        }

        public async Task<UsuarioDto> GetUsuarioByIdAsync(int id)
        {
            var usuario = await _context.Usuario
                .Include(u => u.RolNavigation)
                .FirstOrDefaultAsync(u => u.Id_Usr == id);

            if (usuario == null)
            {
                throw new Exception("Usuario no encontrado");
            }

            return new UsuarioDto
            {
                Id_Usr = usuario.Id_Usr,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Rol = usuario.Rol,
                Estado = usuario.Estado,
                
            };
        }

        public async Task<UsuarioResponse> PerfilUsuario(int usuarioId)
        {
            try
            {
                
                Console.WriteLine($"Buscando perfil para usuarioId: {usuarioId}");

                var usuario = await _context.Usuario
                    .Where(u => u.Id_Usr == usuarioId)
                    .Select(u => new UsuarioResponse
                    {
                        Id_Usr  = u.Id_Usr,
                        Nombre = u.Nombre,
                        Correo = u.Correo,
                        Rol = u.RolNavigation.Nombre_Rol ?? "Sin rol asignado",
                        Estado = u.Estado
                    })
                    .FirstOrDefaultAsync();

                Console.WriteLine($"Usuario encontrado: {(usuario != null ? "SÍ" : "NO")}");
                return usuario;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en servicio: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ActualizarPerfilAsync(UsuarioPerfilDto perfilDto)
        {
            try
            {
                var usuario = await _context.Usuario
                    .FirstOrDefaultAsync(u => u.Id_Usr == perfilDto.Id_Usr);

                if (usuario == null)
                    throw new Exception("Usuario no encontrado");

               
                if (!string.IsNullOrEmpty(perfilDto.Correo))
                {
                    var existeCorreo = await _context.Usuario
                        .AnyAsync(u => u.Correo == perfilDto.Correo && u.Id_Usr != perfilDto.Id_Usr);

                    if (existeCorreo)
                        throw new Exception("El correo electrónico ya está registrado");
                }

                
                if (!string.IsNullOrEmpty(perfilDto.NuevaContrasena))
                {
                    
                    var contraseñaValida = PasswordHelper.VerifyPassword(
                        usuario.Contrasena,
                        perfilDto.ContrasenaActual 
                    );

                    if (!contraseñaValida)
                        throw new Exception("La contraseña actual es incorrecta");

                    
                    usuario.Contrasena = PasswordHelper.HashPassword(perfilDto.NuevaContrasena);
                }

                
                if (!string.IsNullOrEmpty(perfilDto.Nombre))
                {
                    usuario.Nombre = perfilDto.Nombre.Trim();
                }

                if (!string.IsNullOrEmpty(perfilDto.Correo))
                {
                    usuario.Correo = perfilDto.Correo.Trim().ToLower();
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar perfil: {ex.Message}", ex);
            }
        }

    }
    
}


