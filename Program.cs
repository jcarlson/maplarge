using TestProject.Services;

namespace TestProject {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            var baseDirectory = builder.Configuration["FileBrowser:BaseDirectory"]
                ?? throw new InvalidOperationException("FileBrowser:BaseDirectory is not configured.");
            builder.Services.AddSingleton(
                new FileSystemService(Path.GetFullPath(baseDirectory, builder.Environment.ContentRootPath)));

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
