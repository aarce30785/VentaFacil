
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
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InventarioMovimiento>(entity =>
            {
                entity.Property(e => e.Id_Inventario)
                      .HasColumnName("Id_Inventario");
                entity.Property(e => e.Tipo_Movimiento)
                      .HasColumnName("Tipo_Movimiento");
            });

            // NUEVAS CONFIGURACIONES PARA FACTURACIÓN
            ConfigureFacturacionEntities(modelBuilder);
        }

        private void ConfigureFacturacionEntities(ModelBuilder modelBuilder)
        {
            // Configurar entidad Venta
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.HasKey(v => v.Id_Venta);
                entity.Property(v => v.Fecha)
                      .IsRequired();
                entity.Property(v => v.Total)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();
                entity.Property(v => v.MetodoPago)
                      .HasMaxLength(255)
                      .IsRequired(false);
                entity.Property(v => v.Estado)
                      .HasDefaultValue(true);

                // CONFIGURACIÓN EXPLÍCITA DE LA RELACIÓN CON USUARIO
                entity.HasOne(v => v.Usuario)
                      .WithMany() // Usuario no tiene colección de Ventas
                      .HasForeignKey(v => v.Id_Usuario) // Clave foránea en Venta
                      .HasPrincipalKey(u => u.Id_Usr)   // Clave principal en Usuario
                      .OnDelete(DeleteBehavior.Restrict);

                // MAPEAR EXPLÍCITAMENTE LA COLUMNA
                entity.Property(v => v.Id_Usuario)
                      .HasColumnName("Id_Usuario")
                      .IsRequired();
            });

            // Configurar entidad Factura
            modelBuilder.Entity<Factura>(entity =>
            {
                entity.HasKey(f => f.Id_Factura);
                entity.Property(f => f.FechaEmision)
                      .IsRequired();
                entity.Property(f => f.Total)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();
                entity.Property(f => f.Estado)
                      .HasDefaultValue(true);

                // Relación con Venta (uno a uno)
                entity.HasOne(f => f.Venta)
                      .WithOne(v => v.Factura)
                      .HasForeignKey<Factura>(f => f.Id_Venta)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configurar entidad DetalleVenta
            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.HasKey(d => d.Id_Detalle);
                entity.Property(d => d.Cantidad)
                      .IsRequired();
                entity.Property(d => d.PrecioUnitario)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();
                entity.Property(d => d.Descuento)
                      .HasColumnType("decimal(10,2)")
                      .HasDefaultValue(0);

                // Relación con Venta
                entity.HasOne(d => d.Venta)
                      .WithMany(v => v.Detalles)
                      .HasForeignKey(d => d.Id_Venta)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relación con Producto
                entity.HasOne(d => d.Producto)
                      .WithMany()
                      .HasForeignKey(d => d.Id_Producto)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configurar entidad Producto (si no está configurada)
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(p => p.Id_Producto);
                entity.Property(p => p.Nombre)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(p => p.Precio)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();
                entity.Property(p => p.Estado)
                      .HasDefaultValue(true);
            });
        }
    }
}
