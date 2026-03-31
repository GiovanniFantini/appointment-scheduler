using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context, bool seedData = true)
    {
        Console.WriteLine("Checking database migrations...");

        try
        {
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();

            if (pendingMigrations.Any())
            {
                Console.WriteLine($"Found {pendingMigrations.Count} pending migrations:");
                foreach (var migration in pendingMigrations)
                    Console.WriteLine($"  - {migration}");

                Console.WriteLine("Applying migrations...");
                context.Database.Migrate();
                Console.WriteLine("Migrations applied successfully!");
            }
            else
            {
                Console.WriteLine("Database is up to date.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during migration: {ex.Message}");
            throw;
        }

        if (!seedData)
        {
            Console.WriteLine("Seed data creation skipped.");
            return;
        }

        if (context.Users.Any(u => u.AccountType == AccountType.Admin))
        {
            Console.WriteLine("Admin user already exists. Skipping seed.");
            return;
        }

        Console.WriteLine("Creating default admin user...");

        var adminUser = new User
        {
            Email = "admin@admin.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            FirstName = "Admin",
            LastName = "Sistema",
            AccountType = AccountType.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        context.SaveChanges();

        Console.WriteLine("Default admin user created:");
        Console.WriteLine("  Email: admin@admin.com");
        Console.WriteLine("  Password: password");
    }
}
