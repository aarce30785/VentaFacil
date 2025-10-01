using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Helpers;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Admin;

namespace VentaFacil.web.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        

        public AdminService(ApplicationDbContext context)
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

        public async Task<UsuarioResponse> GetUsuarioByIdAsync(int id)
        {
            var usuario = await _context.Usuario
                .Include(u => u.RolNavigation)
                .FirstOrDefaultAsync(u => u.Id_Usr == id);

            if (usuario == null)
            {
                throw new Exception("Usuario no encontrado");
            }

            return new UsuarioResponse
            {
                Id_Usr = usuario.Id_Usr,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Estado = usuario.Estado,
                RolId = usuario.Rol,
                Rol = usuario.RolNavigation?.Nombre_Rol
            };
        }

        public async Task<UsuarioListResponse> GetUsuariosPaginadosAsync(int pagina = 1, int cantidadPorPagina = 10)
        {
            var query = _context.Usuario
                .Include(u => u.RolNavigation)
                .AsQueryable();

            var totalUsuarios = await query.CountAsync();
            var usuarios = await query
                .OrderBy(u => u.Id_Usr) // Agregar ordenamiento
                .Skip((pagina - 1) * cantidadPorPagina)
                .Take(cantidadPorPagina)
                .ToListAsync();

            return new UsuarioListResponse
            {
                Usuarios = usuarios.Select(u => new UsuarioResponse
                {
                    Id_Usr = u.Id_Usr,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    Estado = u.Estado,
                    RolId = u.Rol,
                    Rol = u.RolNavigation?.Nombre_Rol
                }).ToList(),
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling(totalUsuarios / (double)cantidadPorPagina),
                TotalUsuarios = totalUsuarios
            };
        }

        public async Task<bool> CrearUsuarioAsync(UsuarioDto usuarioDto)
        {
            try
            {
                // Verificar si el correo ya existe
                var existeCorreo = await _context.Usuario
                    .AnyAsync(u => u.Correo == usuarioDto.Correo);

                if (existeCorreo)
                {
                    throw new Exception("El correo electrónico ya está registrado");
                }

                // Crear nuevo usuario usando PasswordHelper
                var usuario = new Usuario
                {
                    Nombre = usuarioDto.Nombre.Trim(),
                    Correo = usuarioDto.Correo.Trim().ToLower(),
                    Contrasena = PasswordHelper.HashPassword(usuarioDto.Contrasena), // Usando el Helper
                    Rol = usuarioDto.Rol,
                    Estado = usuarioDto.Estado,
                    FechaCreacion = DateTime.Now,
                   
                };

                _context.Usuario.Add(usuario);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear usuario: {ex.Message}", ex);
            }
        }

        public async Task<bool> ActualizarUsuarioAsync(UsuarioDto usuarioDto)
        {
            try
            {
                var usuario = await _context.Usuario
                    .FirstOrDefaultAsync(u => u.Id_Usr == usuarioDto.Id_Usr);

                if (usuario == null)
                {
                    throw new Exception("Usuario no encontrado");
                }

                // Verificar si el correo ya existe (excluyendo el usuario actual)
                var existeCorreo = await _context.Usuario
                    .AnyAsync(u => u.Correo == usuarioDto.Correo && u.Id_Usr != usuarioDto.Id_Usr);

                if (existeCorreo)
                {
                    throw new Exception("El correo electrónico ya está registrado");
                }

                // Actualizar datos
                usuario.Nombre = usuarioDto.Nombre.Trim();
                usuario.Correo = usuarioDto.Correo.Trim().ToLower();
                usuario.Rol = usuarioDto.Rol;
                usuario.Estado = usuarioDto.Estado;
                

                // Actualizar contraseña solo si se proporcionó una nueva
                if (!string.IsNullOrEmpty(usuarioDto.Contrasena))
                {
                    usuario.Contrasena = PasswordHelper.HashPassword(usuarioDto.Contrasena); // Usando el Helper
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar usuario: {ex.Message}", ex);
            }
        }

        public async Task<bool> EliminarUsuarioAsync(int id)
        {
            try
            {
                var usuario = await _context.Usuario
                    .FirstOrDefaultAsync(u => u.Id_Usr == id);

                if (usuario == null)
                {
                    throw new Exception("Usuario no encontrado");
                }

                _context.Usuario.Remove(usuario);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar usuario: {ex.Message}", ex);
            }
        }
    }
}
