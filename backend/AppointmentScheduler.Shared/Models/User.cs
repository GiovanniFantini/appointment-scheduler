namespace AppointmentScheduler.Shared.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    // Multi-role system: un utente può avere multipli ruoli contemporaneamente
    public bool IsAdmin { get; set; } = false;
    public bool IsConsumer { get; set; } = true; // Default: tutti sono consumer
    public bool IsMerchant { get; set; } = false;
    public bool IsEmployee { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public Merchant? Merchant { get; set; } // Se è un merchant, ha un profilo business
    public ICollection<Employee> Employees { get; set; } = new List<Employee>(); // Un employee può lavorare per multipli merchant
}
