using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
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
                // Ejemplo: si obtienes roles de una base de datos
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
                // Log del error
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
    }
    
}
