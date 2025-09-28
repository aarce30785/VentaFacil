using Microsoft.Data.SqlClient;
using VentaFacil.web.Data;

namespace VentaFacil.web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            bool isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

            if (isRunningInContainer)
            {
                builder.Configuration.AddJsonFile("appsettings.Docker.json", optional: true);
                Console.WriteLine("=== EJECUTANDO EN CONTENEDOR DOCKER ===");
            }
            else
            {
                Console.WriteLine("=== EJECUTANDO EN MODO DESARROLLO LOCAL ===");
            }

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            InitializeDatabase(builder.Configuration);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        private static bool IsRunningInContainer()
        {
            return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        }

        private static void InitializeDatabase(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

            // Conexión a master
            string masterConnection = connectionString.Replace("Database=VentaFacilDB;", "Database=master;");
            using (var connMaster = new SqlConnection(masterConnection))
            {
                connMaster.Open();
                using var cmd = new SqlCommand(
                    "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'VentaFacilDB') CREATE DATABASE VentaFacilDB;",
                    connMaster);
                cmd.ExecuteNonQuery();
            }

            // Conexión a la DB recién creada
            using (var connDb = new SqlConnection(connectionString))
            {
                connDb.Open();

                string scriptPath = "/app/init/init.sql";
                if (!File.Exists(scriptPath))
                    throw new FileNotFoundException($"No se encontró el archivo de inicialización en {scriptPath}");

                string script = File.ReadAllText(scriptPath);

                using var cmd = new SqlCommand(script, connDb);
                cmd.ExecuteNonQuery();
            }

            // Seeder para crear usuario admin
            DbSeeder.Seed(connectionString);

            Console.WriteLine("=== BASE DE DATOS Y TABLAS INICIALIZADAS ===");
        }
    }
}
