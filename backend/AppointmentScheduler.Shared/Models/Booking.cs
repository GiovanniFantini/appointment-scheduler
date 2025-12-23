using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; } // FK a User (cliente)
    public int ServiceId { get; set; } // FK a Service
    public DateTime BookingDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public int NumberOfPeople { get; set; } = 1;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
