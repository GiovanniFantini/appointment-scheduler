using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;
using AppointmentScheduler.API.Constants;

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
        if (context.Users.Any(u => u.Role == UserRole.Admin))
        {
            Console.WriteLine("Admin user already exists. Skipping seed.");
            return; // Il database è già inizializzato
        }

        Console.WriteLine("Creating default admin user...");

        // Leggi credenziali admin dalle variabili d'ambiente o usa i valori di default
        var adminEmail = Environment.GetEnvironmentVariable(ConfigKeys.Environment.AdminDefaultEmail)
            ?? Defaults.Admin.Email;
        var adminPassword = Environment.GetEnvironmentVariable(ConfigKeys.Environment.AdminDefaultPassword)
            ?? Defaults.Admin.Password;
        var adminFirstName = Environment.GetEnvironmentVariable(ConfigKeys.Environment.AdminDefaultFirstName)
            ?? Defaults.Admin.FirstName;
        var adminLastName = Environment.GetEnvironmentVariable(ConfigKeys.Environment.AdminDefaultLastName)
            ?? Defaults.Admin.LastName;

        // Crea utente admin di default
        var adminUser = new User
        {
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            FirstName = adminFirstName,
            LastName = adminLastName,
            PhoneNumber = null,
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        context.SaveChanges();

        Console.WriteLine("Default admin user created successfully!");
        Console.WriteLine($"  Email: {adminEmail}");
        Console.WriteLine($"  Password: {new string('*', adminPassword.Length)} (hidden for security)");
        Console.WriteLine("  Role: Admin");
    }
}
