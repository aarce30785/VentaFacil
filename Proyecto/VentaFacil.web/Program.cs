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
using VentaFacil.web.Services.Movimiento;
using VentaFacil.web.Services.PDF;
using VentaFacil.web.Services.Pedido;
using VentaFacil.web.Services.Producto;
using VentaFacil.web.Services.Usuario;
using VentaFacil.web.Services.Planilla;
using VentaFacil.web.Services.Email;
using VentaFacil.web.Services.Auth;
using VentaFacil.web.Services.BCCR;



namespace VentaFacil.web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Set default culture to es-CR (Costa Rica)
            var cultureInfo = new System.Globalization.CultureInfo("es-CR");
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.UseShutdownTimeout(TimeSpan.FromMinutes(10));
            builder.Host.ConfigureHostOptions(options =>
            {
                options.ShutdownTimeout = TimeSpan.FromMinutes(5);
            });

            bool isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

            // ===== CONFIGURACIÓN DE KESTREL PARA DOCKER =====
            if (isRunningInContainer)
            {
                Console.WriteLine("⚡ Ejecutando en Docker - Usando configuración por defecto (ASPNETCORE_URLS)");
            }

            // ===== CONFIGURACIÓN DE HTTPS / HTTP =====
            // Hostinger -> NO HTTPS interno, NO UseHttpsRedirection
            bool useHttpsRedirection = !isRunningInContainer;

            // ===== CONFIGURACIÓN DE FORWARDED HEADERS =====
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

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
            builder.Services.AddControllersWithViews(options =>
            {
                var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            builder.Services.AddHealthChecks();

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
                   options.LogoutPath = "/Login/CerrarSesion";

                // Cookies seguras solo si el request original fue HTTPS (o estamos en Docker detrás de Nginx)
                options.Cookie.SecurePolicy = (isRunningInContainer && !builder.Environment.IsDevelopment())
                    ? CookieSecurePolicy.Always      
                    : CookieSecurePolicy.SameAsRequest; 

                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.Cookie.SameSite = (isRunningInContainer && !builder.Environment.IsDevelopment())
                    ? SameSiteMode.None
                    : SameSiteMode.Lax; 
               });

            // Configurar antiforgery tokens
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "VentaFacil.Csrf";
                options.Cookie.HttpOnly = true;

                // Igual que cookies: marcar Secure si estamos en Docker tras Nginx
                options.Cookie.SecurePolicy = (isRunningInContainer && !builder.Environment.IsDevelopment())
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;

                options.Cookie.SameSite = (isRunningInContainer && !builder.Environment.IsDevelopment())
                    ? SameSiteMode.None
                    : SameSiteMode.Lax;
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
            builder.Services.AddScoped<IPdfService, PdfService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IPlanillaService, PlanillaService>();
            builder.Services.AddScoped<IBonificacionService, BonificacionService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
            
            // BCCR Service Configuration
            builder.Services.Configure<BccrSettings>(builder.Configuration.GetSection("BccrSettings"));
            builder.Services.AddScoped<IBccrService, BccrService>();

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

                    // Verificar permisos de escritura en directorio existente
                    try
                    {
                        var testFile = Path.Combine(keysDirectory, "test_perm.txt");
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                        Console.WriteLine("=== PERMISOS DE ESCRITURA VERIFICADOS (Directorio Existente) ===");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"=== ⚠️ ERROR DE PERMISOS EN {keysDirectory}: {ex.Message} ===");
                    }

                    // Verificar claves existentes
                    var keyFiles = Directory.GetFiles(keysDirectory, "*.xml");
                    Console.WriteLine($"=== CLAVES ENCONTRADAS: {keyFiles.Length} ===");
                }
            }

            // Configurar Forwarded Headers para Nginx/Apache (PRIMERO)
            var forwardedHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            };
            forwardedHeaderOptions.KnownNetworks.Clear();
            forwardedHeaderOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeaderOptions);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                InitializeDatabase(builder.Configuration);
                 app.UseExceptionHandler("/Home/Error");
                // app.UseHsts(); // Deshabilitado para Docker/Hostinger HTTP simple
            }
            else
            {
                app.UseDeveloperExceptionPage();
                TestDatabaseConnection(builder.Configuration);
                InitializeDatabase(builder.Configuration);
            }

            if (useHttpsRedirection)
            {
                app.UseHttpsRedirection();
            }
            else
            {
                Console.WriteLine("⚠ HTTPS redirection deshabilitado (Hostinger)");
            }
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            provider.Mappings[".css"] = "text/css";
            provider.Mappings[".js"] = "text/javascript";
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = provider
            });

            // Diagnóstico de carpetas estáticas y FORZAR WebRootPath si es necesario
            Console.WriteLine("=== DIAGNÓSTICO DE CARPETAS ESTÁTICAS ===");
            if (isRunningInContainer && (string.IsNullOrEmpty(app.Environment.WebRootPath) || !app.Environment.WebRootPath.Contains("/app/wwwroot")))
            {
                app.Environment.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                Console.WriteLine($"⚠️ WebRootPath forzado a: {app.Environment.WebRootPath}");
            }

            string wwwroot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            Console.WriteLine($"WebRootPath actual: {wwwroot}");

            string[] checkFolders = { 
                wwwroot, 
                Path.Combine(wwwroot, "css"),
                Path.Combine(wwwroot, "images"), 
                Path.Combine(wwwroot, "images", "productos"),
                Path.Combine(wwwroot, "Plantilla"),
                Path.Combine(wwwroot, "Plantilla", "css")
            };

            foreach (var folder in checkFolders)
            {
                bool exists = Directory.Exists(folder);
                Console.WriteLine($"Folder: {folder} - Exists: {exists}");
                if (exists)
                {
                    try {
                        var files = Directory.GetFiles(folder);
                        Console.WriteLine($"   Files found: {files.Length}");
                        foreach (var file in files.Take(5)) // Mostrar hasta 5 archivos para no inundar el log
                        {
                            Console.WriteLine($"     - {Path.GetFileName(file)}");
                        }
                        if (files.Length > 5) Console.WriteLine("     ...");
                    } catch (Exception ex) {
                        Console.WriteLine($"   Error reading: {ex.Message}");
                    }
                }
            }

            app.UseRouting();



            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();
            app.UseMiddleware<VentaFacil.web.Middleware.SessionValidationMiddleware>();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHealthChecks("/health");

            app.Run();
        }

        private static void InitializeDatabase(IConfiguration configuration)
        {
            Console.WriteLine("=== INICIANDO INICIALIZACIÓN DE BD ===");

            string connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

            Console.WriteLine($"Cadena de conexión: {connectionString.Replace("Password=VentaFacilDb123!", "Password=***")}");

            // Intentar conexión a la BD
            // Intentar conexión a la BD con reintentos
            int maxRetries = 10; // 10 intentos
            int delaySeconds = 60; // 60 segundos (1 minuto) entre intentos

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Console.WriteLine($"Intento de conexión {i + 1} de {maxRetries}...");

                    // 1. Conectar a master para verificar/crear la BD
                    var builder = new SqlConnectionStringBuilder(connectionString);
                    string targetDatabase = builder.InitialCatalog;
                    builder.InitialCatalog = "master"; // Cambiar a master

                    using (var masterConn = new SqlConnection(builder.ConnectionString))
                    {
                        masterConn.Open();
                        Console.WriteLine("✅ Conexión a 'master' exitosa");

                        // Verificar si existe la BD
                        var checkDbCmd = new SqlCommand($"SELECT database_id FROM sys.databases WHERE Name = '{targetDatabase}'", masterConn);
                        var dbId = checkDbCmd.ExecuteScalar();

                        if (dbId == null)
                        {
                            Console.WriteLine($"⚠️ Base de datos '{targetDatabase}' no existe. Creando...");
                            var createDbCmd = new SqlCommand($"CREATE DATABASE [{targetDatabase}]", masterConn);
                            createDbCmd.ExecuteNonQuery();
                            Console.WriteLine($"✅ Base de datos '{targetDatabase}' CREADA exitosamente");
                            
                            // Esperar un momento para que la BD esté disponible
                            System.Threading.Thread.Sleep(2000); 
                        }
                        else
                        {
                            Console.WriteLine($"✅ Base de datos '{targetDatabase}' ya existe");
                        }
                    }

                    // 2. Conectar a la base de datos correcta (VentaFacilDB)
                    using (var conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        Console.WriteLine($"✅ Conexión a {targetDatabase} exitosa");

                        // Si llegamos aquí, salimos del bucle de reintentos
                        // Continuar con la inicialización...
                        InitializeTablesAndSeed(conn, connectionString);
                        return; // Éxito
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Intento {i + 1} fallido. Esperando {delaySeconds}s... Error: {ex.Message}");
                    if (i == maxRetries - 1)
                    {
                        Console.WriteLine("❌ Se agotaron los reintentos de conexión.");
                        Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(delaySeconds * 1000);
                    }
                }
            }
        }

        private static void InitializeTablesAndSeed(SqlConnection conn, string connectionString)
        {
            Console.WriteLine("=== [DB INIT] INICIANDO VERIFICACIÓN DE ESQUEMA ===");
            
            string baseDir = Directory.GetCurrentDirectory();
            string[] potentialPaths = {
                "/app/init/init.sql",
                Path.Combine(baseDir, "init", "init.sql"),
                Path.Combine(baseDir, "init.sql"),
                "init.sql"
            };

            string? scriptPath = potentialPaths.FirstOrDefault(File.Exists);

            if (scriptPath != null)
            {
                Console.WriteLine($"=== [DB INIT] Script encontrado en: {scriptPath}");
                string script = File.ReadAllText(scriptPath);
                try
                {
                    // Dividir el script por 'GO' para ejecución por lotes
                    string[] batches = System.Text.RegularExpressions.Regex.Split(
                        script,
                        @"^\s*GO\s*$",
                        System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    Console.WriteLine($"=== [DB INIT] Ejecutando {batches.Length} lotes de comandos...");
                    int successCount = 0;

                    foreach (var batch in batches)
                    {
                        if (string.IsNullOrWhiteSpace(batch)) continue;

                        try
                        {
                            using var cmd = new SqlCommand(batch, conn);
                            cmd.ExecuteNonQuery();
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ [DB INIT] Error en lote {successCount + 1}: {ex.Message}");
                            string snippet = batch.Length > 100 ? batch.Substring(0, 100).Trim() + "..." : batch.Trim();
                            Console.WriteLine($"   Comando: {snippet}");
                        }
                    }
                    Console.WriteLine($"=== [DB INIT] Proceso completado. Lotes exitosos: {successCount}/{batches.Length}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [DB INIT] Error crítico leyendo/procesando script: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("❌ [DB INIT] ERROR: No se encontró el script init.sql en ninguna de las rutas:");
                foreach (var path in potentialPaths) Console.WriteLine($"   - {path}");
            }

            Console.WriteLine("=== [DB INIT] Ejecutando seeder de datos iniciales...");
            try
            {
                DbSeeder.Seed(connectionString);
                Console.WriteLine("✅ [DB INIT] Seeder completado");
            }
            catch (Exception seederEx)
            {
                Console.WriteLine($"⚠️ [DB INIT] Error en seeder: {seederEx.Message}");
            }
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
