using Microsoft.EntityFrameworkCore;

namespace VentaFacil.web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        // Define your DbSets here. For example:
        // public DbSet<YourEntity> YourEntities { get; set; }
    }
}
