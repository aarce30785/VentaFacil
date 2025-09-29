using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VentaFacil.web.Data;
using VentaFacil.web.Services;

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

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            if (isRunningInContainer)
            {
                // En Docker, usar un directorio persistente para las claves
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
                    .SetApplicationName("VentaFacil")
                    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
            }
            else
            {
                // En desarrollo local, usar el directorio por defecto
                builder.Services.AddDataProtection()
                    .SetApplicationName("VentaFacil");
            }


            builder.Services.AddScoped<IAuthService, AuthService>();

            // Configurar sesión
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Configurar autenticación con cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(options =>
               {
                   options.LoginPath = "/Login/InicioSesion";
                   options.AccessDeniedPath = "/Login/AccessDenied";
                   options.LogoutPath = "/Login/Logout";
                   options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                   options.SlidingExpiration = true;
                   options.Cookie.Name = "VentaFacil.Auth";
                   options.Cookie.HttpOnly = true;
                   options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                   options.Cookie.SameSite = SameSiteMode.Strict;
               });

            // Configurar antiforgery tokens
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "VentaFacil.Csrf";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            // Configurar autorización
            builder.Services.AddAuthorization();

            var app = builder.Build();

            if (isRunningInContainer)
            {
                var keysDirectory = "/app/keys";
                if (!Directory.Exists(keysDirectory))
                {
                    Directory.CreateDirectory(keysDirectory);
                    Console.WriteLine($"=== DIRECTORIO DE CLAVES CREADO: {keysDirectory} ===");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                InitializeDatabase(builder.Configuration);
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                
                InitializeDatabase(builder.Configuration);
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
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
