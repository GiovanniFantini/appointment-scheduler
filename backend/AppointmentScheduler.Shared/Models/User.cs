using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public Merchant? Merchant { get; set; } // Se è un merchant, ha un profilo business
    public Employee? Employee { get; set; } // Se è un employee, ha un profilo dipendente
}
