using TestProject.Services;

namespace TestProject
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            var baseDirectory = builder.Configuration["FileBrowser:BaseDirectory"]
                ?? throw new InvalidOperationException("FileBrowser:BaseDirectory is not configured.");
            builder.Services.AddSingleton<IFileSystemService>(
                new FileSystemService(Path.GetFullPath(baseDirectory, builder.Environment.ContentRootPath)));

            var app = builder.Build();

            // "/" serves wwwroot/index.html, the SPA; everything else under wwwroot is served as is.
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
