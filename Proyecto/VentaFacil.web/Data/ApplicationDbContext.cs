
﻿using Microsoft.EntityFrameworkCore;
﻿using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using VentaFacil.web.Models;

namespace VentaFacil.web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        // Entidades existentes
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Rol> Rol { get; set; }
        public DbSet<Categoria> Categoria { get; set; }
        public DbSet<Producto> Producto { get; set; }
        public DbSet<Inventario> Inventario { get; set; }
        public DbSet<InventarioMovimiento> InventarioMovimiento { get; set; }
        public DbSet<Caja> Caja { get; set; }
        public DbSet<CajaRetiro> CajaRetiro { get; set; }

        // NUEVAS ENTIDADES PARA FACTURACIÓN
        public DbSet<Venta> Venta { get; set; }
        public DbSet<Factura> Factura { get; set; }
        public DbSet<DetalleVenta> DetalleVenta { get; set; }

        public DbSet<Nomina> Nomina { get; set; }
        public DbSet<Planilla> Planilla { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones existentes...
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.HasKey(r => r.Id_Rol);
                entity.Property(r => r.Nombre_Rol)
                      .IsRequired()
                      .HasMaxLength(50);
                entity.Property(r => r.Descripcion)
                      .HasMaxLength(200);
            });

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

                entity.HasOne(u => u.RolNavigation)
                      .WithMany(r => r.Usuarios)
                      .HasForeignKey(u => u.Rol)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired();
            });

            modelBuilder.Entity<InventarioMovimiento>(entity =>
            {
                entity.Property(e => e.Id_Inventario)
                      .HasColumnName("Id_Inventario");
                entity.Property(e => e.Tipo_Movimiento)
                      .HasColumnName("Tipo_Movimiento");
            });

            

            
        }

      
    }
}
