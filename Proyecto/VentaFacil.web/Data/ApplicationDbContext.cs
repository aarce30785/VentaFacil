
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Dto;

namespace VentaFacil.web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
      


        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Rol> Rol { get; set; }
        public DbSet<Categoria> Categoria { get; set; }
        public DbSet<Producto> Producto { get; set; }
        public DbSet<Inventario> Inventario { get; set; }
        public DbSet<InventarioMovimiento> InventarioMovimiento { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            

            // Configurar entidad Rol
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.HasKey(r => r.Id_Rol);
                entity.Property(r => r.Nombre_Rol)
                      .IsRequired()
                      .HasMaxLength(50);
                entity.Property(r => r.Descripcion)
                      .HasMaxLength(200);
            });

            // Configurar entidad Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(u => u.Id_Usr);
                entity.Property(u => u.Nombre)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(u => u.Correo)
                      .IsRequired()
                      .HasMaxLength(150);
                entity.Property(u => u.Contrasena)
                      .IsRequired();
                entity.Property(u => u.Estado)
                      .HasDefaultValue(true);

                // Configurar relación con Rol
                entity.HasOne(u => u.RolNavigation)
                      .WithMany(r => r.Usuarios)
                      .HasForeignKey(u => u.Rol)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
        public DbSet<VentaFacil.web.Models.Dto.InventarioDto> InventarioDto { get; set; } = default!;
        public DbSet<VentaFacil.web.Models.Dto.InventarioMovimientoDto> InventarioMovimientoDto { get; set; } = default!;
        //public DbSet<VentaFacil.web.Models.Dto.ProductoDto> ProductoDto { get; set; } = default!;

        
    }

}
