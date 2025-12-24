using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AppointmentScheduler.Data;

/// <summary>
/// Factory per creare ApplicationDbContext durante il design-time (migrations)
/// Questo permette a EF Core Tools di creare il DbContext senza dover costruire l'intera applicazione
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Crea un'istanza di ApplicationDbContext per EF Core Tools
    /// </summary>
    /// <param name="args">Argomenti da command line</param>
    /// <returns>Istanza di ApplicationDbContext configurata</returns>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        Console.WriteLine("=== DesignTimeDbContextFactory: Creating DbContext for migrations ===");
        Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

        // Cerca il file appsettings.json risalendo nella gerarchia delle directory
        var currentDir = Directory.GetCurrentDirectory();
        string? basePath = null;

        // Prova diversi percorsi relativi
        var possiblePaths = new[]
        {
            Path.Combine(currentDir, "..", "AppointmentScheduler.API"),
            Path.Combine(currentDir, "AppointmentScheduler.API"),
            Path.Combine(currentDir, "..", "..", "AppointmentScheduler.API"),
            Path.Combine(currentDir, "..", "..", "backend", "AppointmentScheduler.API")
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            var settingsFile = Path.Combine(fullPath, "appsettings.json");
            Console.WriteLine($"Trying: {settingsFile}");

            if (File.Exists(settingsFile))
            {
                basePath = fullPath;
                Console.WriteLine($"Found appsettings.json at: {basePath}");
                break;
            }
        }

        if (basePath == null)
        {
            throw new InvalidOperationException(
                $"Could not find appsettings.json. Current directory: {currentDir}");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        Console.WriteLine($"Connection string loaded: {!string.IsNullOrEmpty(connectionString)}");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration. " +
                "Make sure appsettings.json exists in the API project.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        Console.WriteLine("=== DbContext created successfully ===");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
