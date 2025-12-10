using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polly;
using Microsoft.AspNetCore.HttpOverrides;
using VentaFacil.web.Data;
using VentaFacil.web.Services;
using VentaFacil.web.Services.Admin;
using VentaFacil.web.Services.Caja;
using VentaFacil.web.Services.Categoria;
using VentaFacil.web.Services.Facturacion;
using VentaFacil.web.Services.Inventario;
using VentaFacil.web.Services.Inventario;
using VentaFacil.web.Services.Movimiento;
using VentaFacil.web.Services.Movimiento;
using VentaFacil.web.Services.PDF;
using VentaFacil.web.Services.Pedido;
using VentaFacil.web.Services.Producto;
using VentaFacil.web.Services.Usuario;
using VentaFacil.web.Services.Planilla;
using VentaFacil.web.Services.Email;
using VentaFacil.web.Services.Auth;


namespace VentaFacil.web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.UseShutdownTimeout(TimeSpan.FromMinutes(10));
            builder.Host.ConfigureHostOptions(options =>
            {
                options.ShutdownTimeout = TimeSpan.FromMinutes(5);
            });

            bool isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

            var environment = builder.Environment;

            Console.WriteLine($"=== ENTORNO: {environment.EnvironmentName} ===");
            Console.WriteLine($"=== EN CONTENEDOR: {isRunningInContainer} ===");

            if (isRunningInContainer)
            {
                builder.Configuration.AddJsonFile("appsettings.Docker.json", optional: true);
                Console.WriteLine("=== EJECUTANDO EN CONTENEDOR DOCKER ===");
            }
            else
            {
                Console.WriteLine("=== EJECUTANDO EN MODO DESARROLLO LOCAL ===");
            }

            if (isRunningInContainer)
            {
                // En Docker, usar un directorio persistente para las claves
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
                    .SetApplicationName("VentaFacil")
                    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

                Console.WriteLine("=== DATA PROTECTION CONFIGURADO PARA DOCKER ===");
            }
            else
            {
                // En desarrollo local, usar el directorio por defecto
                builder.Services.AddDataProtection()
                    .SetApplicationName("VentaFacil");

                Console.WriteLine("=== DATA PROTECTION CONFIGURADO PARA DESARROLLO LOCAL ===");
            }


            // Add services to the container.
            builder.Services.AddControllersWithViews().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null
                )
            ));

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


            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUsuarioService, UsuarioService>();
            builder.Services.AddScoped<ICategoriaService, CategoriaService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IPedidoService, PedidoService>();
            builder.Services.AddScoped<IProductoService, ProductoService>();
            builder.Services.AddScoped<IInventarioService, InventarioService>();
            builder.Services.AddScoped<IMovimientoService, MovimientoService>();
            builder.Services.AddScoped<IFacturacionService, FacturacionService>();
            builder.Services.AddScoped<ICajaService, CajaService>();
            builder.Services.AddScoped<PdfService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IPlanillaService, PlanillaService>();
            builder.Services.AddScoped<IBonificacionService, BonificacionService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

            // Configurar sesión


            var app = builder.Build();

            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() => {
                Console.WriteLine("🛑 APPLICATION STOPPING - Shutdown requested");
            });

            lifetime.ApplicationStopped.Register(() => {
                Console.WriteLine("🛑 APPLICATION STOPPED");
            });

            if (isRunningInContainer)
            {
                var keysDirectory = "/app/keys";
                if (!Directory.Exists(keysDirectory))
                {
                    Directory.CreateDirectory(keysDirectory);
                    Console.WriteLine($"=== DIRECTORIO DE CLAVES CREADO: {keysDirectory} ===");

                    // Verificar permisos
                    try
                    {
                        var testFile = Path.Combine(keysDirectory, "test.txt");
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                        Console.WriteLine("=== PERMISOS DE ESCRITURA VERIFICADOS ===");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"=== ERROR DE PERMISOS EN {keysDirectory}: {ex.Message} ===");
                    }
                }
                else
                {
                    Console.WriteLine($"=== DIRECTORIO DE CLAVES YA EXISTE: {keysDirectory} ===");

                    // Verificar claves existentes
                    var keyFiles = Directory.GetFiles(keysDirectory, "*.xml");
                    Console.WriteLine($"=== CLAVES ENCONTRADAS: {keyFiles.Length} ===");
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
                TestDatabaseConnection(builder.Configuration);
                //InitializeDatabase(builder.Configuration);
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Configurar Forwarded Headers para Nginx/Apache
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            });

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
            Console.WriteLine("=== INICIANDO INICIALIZACIÓN DE BD ===");
            
            string connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

            Console.WriteLine($"Cadena de conexión: {connectionString.Replace("Password=VentaFacilDb123!", "Password=***")}");

            // Conexión a master
            string masterConnection = connectionString.Replace("Database=VentaFacilDB;", "Database=master;");

            var policy = Policy
                .Handle<SqlException>()
                .Or<InvalidOperationException>()
                .WaitAndRetry(
                    retryCount: 10,  // Aumentar a 10 intentos
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"❌ Intento {retryCount} de conexión a SQL Server falló. Reintentando en {timeSpan.Seconds} segundos...");
                        Console.WriteLine($"Error: {exception.Message}");
                    });

            try
            {
                policy.Execute(() =>
                {
                    Console.WriteLine($"Intentando conexión a master...");
                    using (var connMaster = new SqlConnection(masterConnection))
                    {
                        connMaster.Open();
                        Console.WriteLine("✅ Conexión a SQL Server (master) exitosa");

                        // Verificar si la base de datos existe
                        using var cmdCheckDb = new SqlCommand(
                            "SELECT COUNT(*) FROM sys.databases WHERE name = 'VentaFacilDB'",
                            connMaster);
                        var dbExists = (int)cmdCheckDb.ExecuteScalar() > 0;

                        if (!dbExists)
                        {
                            Console.WriteLine("Creando base de datos VentaFacilDB...");
                            using var cmdCreateDb = new SqlCommand(
                                "CREATE DATABASE VentaFacilDB;",
                                connMaster);
                            cmdCreateDb.ExecuteNonQuery();
                            Console.WriteLine("✅ Base de datos creada");

                            // Esperar más tiempo después de crear la BD
                            Thread.Sleep(5000);
                        }
                        else
                        {
                            Console.WriteLine("✅ Base de datos ya existe");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR CRÍTICO: No se pudo inicializar la base de datos: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                // No lanzar excepción, dejar que la aplicación continúe
                return;
            }

            // Esperar antes de conectar a la nueva BD
            Thread.Sleep(3000);

            try
            {
                // Conexión a la DB específica
                policy.Execute(() =>
                {
                    Console.WriteLine($"Intentando conexión a VentaFacilDB...");
                    using (var connDb = new SqlConnection(connectionString))
                    {
                        connDb.Open();
                        Console.WriteLine("✅ Conexión a VentaFacilDB exitosa");

                        string scriptPath = "/app/init/init.sql";
                        if (!File.Exists(scriptPath))
                        {
                            Console.WriteLine($"⚠️ No se encontró el archivo de inicialización en {scriptPath}");
                            return;
                        }

                        Console.WriteLine($"Ejecutando script: {scriptPath}");
                        string script = File.ReadAllText(scriptPath);

                        // Dividir por punto y coma
                        var batches = script.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                        .Where(b => !string.IsNullOrWhiteSpace(b))
                                        .Select(b => b.Trim());

                        int batchCount = 0;
                        foreach (var batch in batches)
                        {
                            if (!string.IsNullOrWhiteSpace(batch))
                            {
                                batchCount++;
                                try
                                {
                                    using var cmd = new SqlCommand(batch, connDb);
                                    cmd.ExecuteNonQuery();
                                    Console.WriteLine($"✅ Batch {batchCount} ejecutado");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"⚠️ Error en batch {batchCount}: {ex.Message}");
                                    Console.WriteLine($"Batch: {batch.Substring(0, Math.Min(100, batch.Length))}...");
                                }
                            }
                        }

                        Console.WriteLine($"✅ {batchCount} scripts SQL ejecutados");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ERROR ejecutando scripts: {ex.Message}");
            }

            // Intentar seeder
            try
            {
                Console.WriteLine("Ejecutando seeder...");
                DbSeeder.Seed(connectionString);
                Console.WriteLine("✅ Seeder ejecutado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ ERROR en seeder: {ex.Message}");
            }

            Console.WriteLine("=== INICIALIZACIÓN DE BD COMPLETADA ===");
        }

        private static void TestDatabaseConnection(IConfiguration configuration)
        {
            try
            {
                string connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("=== ADVERTENCIA: No hay cadena de conexión configurada ===");
                    return;
                }

                Console.WriteLine($"=== INTENTANDO CONEXIÓN CON: {connectionString} ===");

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                // Probar consulta simple
                using var cmd = new SqlCommand("SELECT DB_NAME()", connection);
                var dbName = cmd.ExecuteScalar();

                Console.WriteLine($"=== CONEXIÓN A BD EXITOSA - Base de datos: {dbName} ===");

                // Verificar tablas
                using var cmdTables = new SqlCommand(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
                    connection);
                var tableCount = cmdTables.ExecuteScalar();
                Console.WriteLine($"=== TABLAS EN LA BASE DE DATOS: {tableCount} ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR DE CONEXIÓN A BD: {ex.Message} ===");
                Console.WriteLine($"=== StackTrace: {ex.StackTrace} ===");
            }
        }
    }
}
