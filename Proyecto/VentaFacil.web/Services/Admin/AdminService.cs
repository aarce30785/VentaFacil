using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Helpers;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;
using VentaFacil.web.Models.Response.Admin;
using VentaFacil.web.Models.Response.Usuario;

namespace VentaFacil.web.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UsuarioListResponse> GetUsuariosPaginadosAsync(int pagina, int cantidadPorPagina, string busqueda = null, int? rolFiltro = null)
        {
            try
            {
                var query = _context.Usuario
                    .Include(u => u.RolNavigation)
                    .AsQueryable();

                // Aplicar filtro de búsqueda
                if (!string.IsNullOrEmpty(busqueda))
                {
                    busqueda = busqueda.Trim().ToLower();
                    query = query.Where(u =>
                        u.Nombre.ToLower().Contains(busqueda) ||
                        u.Correo.ToLower().Contains(busqueda));
                }

                // Aplicar filtro por rol
                if (rolFiltro.HasValue && rolFiltro.Value > 0)
                {
                    query = query.Where(u => u.Rol == rolFiltro.Value);
                }

                var totalUsuarios = await query.CountAsync();
                var totalPaginas = (int)Math.Ceiling(totalUsuarios / (double)cantidadPorPagina);

                var usuarios = await query
                    .OrderBy(u => u.Nombre)
                    .Skip((pagina - 1) * cantidadPorPagina)
                    .Take(cantidadPorPagina)
                    .Select(u => new UsuarioResponse
                    {
                        Id_Usr = u.Id_Usr,
                        Nombre = u.Nombre,
                        Correo = u.Correo,
                        Estado = u.Estado,
                        RolId = u.Rol,
                        Rol = u.RolNavigation.Nombre_Rol
                    })
                    .ToListAsync();

                return new UsuarioListResponse
                {
                    Usuarios = usuarios,
                    PaginaActual = pagina,
                    TotalPaginas = totalPaginas,
                    TotalUsuarios = totalUsuarios,
                    UsuarioSeleccionado = null,
                    AccionModal = null,
                    Busqueda = busqueda,
                    RolFiltro = rolFiltro
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener usuarios paginados", ex);
            }
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
                var usuario = new VentaFacil.web.Models.Usuario
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
                    throw new Exception("Usuario no encontrado");

                // Verificar si el correo ya existe (excluyendo el usuario actual)
                var existeCorreo = await _context.Usuario
                    .AnyAsync(u => u.Correo == usuarioDto.Correo && u.Id_Usr != usuarioDto.Id_Usr);

                if (existeCorreo)
                    throw new Exception("El correo electrónico ya está registrado");

                // Actualizar datos básicos
                usuario.Nombre = usuarioDto.Nombre.Trim();
                usuario.Correo = usuarioDto.Correo.Trim().ToLower();
                usuario.Rol = usuarioDto.Rol;
                usuario.Estado = usuarioDto.Estado;

                // Actualizar contraseña si viene una nueva
                if (!string.IsNullOrEmpty(usuarioDto.Contrasena))
                {
                    usuario.Contrasena = PasswordHelper.HashPassword(usuarioDto.Contrasena);
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
