using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Models;
using VentaFacil.web.Models.Enum;
using VentaFacil.web.Models.Configuracion;

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
        public DbSet<InventarioMovimientoAuditoria> InventarioMovimientoAuditoria { get; set; }
        public DbSet<Caja> Caja { get; set; }
        public DbSet<CajaRetiro> CajaRetiro { get; set; }

        // NUEVAS ENTIDADES PARA FACTURACIÓN
        public DbSet<Venta> Venta { get; set; }
        public DbSet<Factura> Factura { get; set; }
        public DbSet<DetalleVenta> DetalleVenta { get; set; }
        public DbSet<PagoFactura> PagoFactura { get; set; }

        public DbSet<Nomina> Nomina { get; set; }
        public DbSet<Planilla> Planilla { get; set; }

        // CONFIGURACIÓN
        public DbSet<DeduccionLey> DeduccionLey { get; set; }
        public DbSet<ImpuestoRenta> ImpuestoRenta { get; set; }

        // BONIFICACIONES
        public DbSet<Bonificacion> Bonificacion { get; set; }
        public DbSet<BonificacionAuditoria> BonificacionAuditoria { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // CONFIGURACIÓN MÍNIMA PARA VENTA - SIN RELACIONES
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.ToTable("Venta");
                entity.HasKey(v => v.Id_Venta);

                // Solo mapear las columnas que existen en la BD
                entity.Property(v => v.Fecha).IsRequired();
                entity.Property(v => v.Total)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)");
                entity.Property(v => v.MetodoPago)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(v => v.Estado)
                      .HasDefaultValue(true);
                entity.Property(v => v.Id_Usuario)
                      .IsRequired();

                // NO configurar relaciones por ahora
            });

            // Configuración para Factura
            modelBuilder.Entity<Factura>(entity =>
            {
                entity.ToTable("Factura");
                entity.HasKey(f => f.Id_Factura);

                entity.Property(f => f.Id_Venta).IsRequired();
                entity.Property(f => f.Cliente)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(f => f.FechaEmision).IsRequired();
                entity.Property(f => f.Total)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)");
                entity.Property(f => f.MontoPagado)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)")
                      .HasDefaultValue(0);
                entity.Property(f => f.Cambio)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)")
                      .HasDefaultValue(0);
                entity.Property(f => f.Moneda)
                      .IsRequired()
                      .HasMaxLength(3)
                      .HasDefaultValue("CRC");
                entity.Property(f => f.MetodoPago)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue("Efectivo");
                entity.Property(f => f.TasaCambio)
                      .HasColumnType("decimal(10,4)");
                entity.Property(f => f.Estado)
                      .HasDefaultValue(EstadoFactura.Activa);
            });

            // Configuración para DetalleVenta
            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.ToTable("DetalleVenta");
                entity.HasKey(d => d.Id_Detalle);

                entity.Property(d => d.Id_Venta).IsRequired();
                entity.Property(d => d.Id_Producto).IsRequired();
                entity.Property(d => d.Cantidad).IsRequired();
                entity.Property(d => d.PrecioUnitario)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)");
                entity.Property(d => d.Descuento)
                      .HasColumnType("decimal(10,2)");
            });

            // Configuración para PagoFactura
            modelBuilder.Entity<PagoFactura>(entity =>
            {
                entity.ToTable("PagoFactura");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.FacturaId).IsRequired();
                entity.Property(p => p.MetodoPago)
                      .IsRequired()
                      .HasMaxLength(20);
                entity.Property(p => p.Monto)
                      .IsRequired()
                      .HasColumnType("decimal(10,2)");
                entity.Property(p => p.Moneda)
                      .IsRequired()
                      .HasMaxLength(3)
                      .HasDefaultValue("CRC");
                entity.Property(p => p.TasaCambio)
                      .HasColumnType("decimal(10,4)");

                entity.HasOne(p => p.Factura)
                      .WithMany(f => f.Pagos)
                      .HasForeignKey(p => p.FacturaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuraciones existentes para otras entidades...
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
        }
    }
}
