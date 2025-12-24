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
        // Build configuration dalla directory API (dove si trova appsettings.json)
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "AppointmentScheduler.API");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration. " +
                "Make sure appsettings.json exists in the API project.");
        }

        // Usa PostgreSQL (come configurato in Program.cs)
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
