using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Data;

/// <summary>
/// Inizializza il database con migrazione e dati di seed
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Applica le migrazioni al database e opzionalmente inizializza con dati di seed
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="seedData">Se true, crea l'utente admin di default (default: true)</param>
    public static void Initialize(ApplicationDbContext context, bool seedData = true)
    {
        Console.WriteLine("Checking database migrations...");

        try
        {
            // Applica tutte le migrazioni pending automaticamente
            // Questo è sicuro da eseguire anche in produzione se il database esiste già
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();

            if (pendingMigrations.Any())
            {
                Console.WriteLine($"Found {pendingMigrations.Count} pending migrations:");
                foreach (var migration in pendingMigrations)
                {
                    Console.WriteLine($"  - {migration}");
                }

                Console.WriteLine("Applying migrations...");
                context.Database.Migrate();
                Console.WriteLine("Migrations applied successfully!");
            }
            else
            {
                Console.WriteLine("Database is up to date. No pending migrations.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during migration: {ex.Message}");
            throw;
        }

        // Seed dei dati solo se richiesto
        if (!seedData)
        {
            Console.WriteLine("Seed data creation skipped (seedData=false).");
            return;
        }

        // Controlla se esiste già un admin
        if (context.Users.Any(u => u.IsAdmin))
        {
            Console.WriteLine("Admin user already exists. Skipping seed.");
            return; // Il database è già inizializzato
        }

        Console.WriteLine("Creating default admin user...");

        // Crea utente admin di default
        var adminUser = new User
        {
            Email = "admin@admin.com".ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), // Password: "password"
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = null,
            IsAdmin = true,
            IsConsumer = true, // Admin può fare tutto
            IsMerchant = true,
            IsEmployee = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        context.SaveChanges();

        Console.WriteLine("Default admin user created successfully!");
        Console.WriteLine("  Email: admin@admin.com");
        Console.WriteLine("  Password: password");
        Console.WriteLine("  Roles: Admin, Consumer, Merchant, Employee (full access)");
    }
}
