using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Data;

/// <summary>
/// Inizializza il database con dati di seed per development/testing
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Inizializza il database con un utente admin di default
    /// </summary>
    /// <param name="context">Database context</param>
    public static void Initialize(ApplicationDbContext context)
    {
        // Assicurati che il database sia creato
        context.Database.EnsureCreated();

        // Controlla se esiste già un admin
        if (context.Users.Any(u => u.Role == UserRole.Admin))
        {
            Console.WriteLine("Admin user already exists. Skipping seed.");
            return; // Il database è già inizializzato
        }

        Console.WriteLine("Creating default admin user...");

        // Crea utente admin di default
        var adminUser = new User
        {
            Email = "admin@admin.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), // Password: "password"
            FirstName = "Admin",
            LastName = "User",
            PhoneNumber = null,
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        context.SaveChanges();

        Console.WriteLine("Default admin user created successfully!");
        Console.WriteLine("  Email: admin@admin.com");
        Console.WriteLine("  Password: password");
        Console.WriteLine("  Role: Admin");
    }
}
