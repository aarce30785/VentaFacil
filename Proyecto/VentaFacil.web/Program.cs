namespace VentaFacil.web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();


            // Deshabilitar HTTPS redirection en contenedores
            if (IsRunningInContainer())
            {
                builder.WebHost.UseUrls("http://*:8080");
            }

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (!IsRunningInContainer())
            {
                app.UseHttpsRedirection();
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
    }
}
