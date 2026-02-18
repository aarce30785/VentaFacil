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

        public async Task<UsuarioListResponse> GetUsuariosPaginadosAsync(int pagina, int cantidadPorPagina, string busqueda = null, int? rolFiltro = null, bool mostrarInactivos = false)
        {
            try
            {
                var query = _context.Usuario
                    .Include(u => u.RolNavigation)
                    .AsQueryable();

                // Filtrar por estado activo/inactivo
                if (!mostrarInactivos)
                {
                    query = query.Where(u => u.Estado);
                }

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
                
                var existeCorreo = await _context.Usuario
                    .AnyAsync(u => u.Correo == usuarioDto.Correo);

                if (existeCorreo)
                {
                    throw new Exception("El correo electrónico ya está registrado");
                }

                
                var usuario = new VentaFacil.web.Models.Usuario
                {
                    Nombre = usuarioDto.Nombre.Trim(),
                    Correo = usuarioDto.Correo.Trim().ToLower(),
                    // Generar una contraseña aleatoria compleja temporalmente
                    Contrasena = PasswordHelper.HashPassword(Guid.NewGuid().ToString() + "A1!"),
                    Rol = usuarioDto.Rol,
                    Estado = false, // Inactivo por defecto hasta que active su cuenta
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

                
                var existeCorreo = await _context.Usuario
                    .AnyAsync(u => u.Correo == usuarioDto.Correo && u.Id_Usr != usuarioDto.Id_Usr);

                if (existeCorreo)
                    throw new Exception("El correo electrónico ya está registrado");

                
                usuario.Nombre = usuarioDto.Nombre.Trim();
                usuario.Correo = usuarioDto.Correo.Trim().ToLower();
                usuario.Rol = usuarioDto.Rol;
                usuario.Estado = usuarioDto.Estado;

                
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

                // Soft delete: Cambiar estado a inactivo en lugar de eliminar
                usuario.Estado = false;
                _context.Usuario.Update(usuario);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar usuario: {ex.Message}", ex);
            }
        }

        public async Task<bool> ReactivarUsuarioAsync(int id)
        {
            try
            {
                var usuario = await _context.Usuario
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id_Usr == id);

                if (usuario == null)
                {
                    throw new Exception("Usuario no encontrado");
                }

                if (usuario.Estado)
                {
                    throw new Exception("El usuario ya se encuentra activo");
                }

                usuario.Estado = true;
                _context.Usuario.Update(usuario);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al reactivar usuario: {ex.Message}", ex);
            }
        }
    }
}
