namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Represents the standard weekly business hours for a service.
/// Allows defining recurring schedules (e.g., "Open Tuesday-Sunday 12:00-15:00 and 19:00-23:00, Closed Monday")
/// </summary>
public class BusinessHours
{
    public int Id { get; set; }

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    /// <summary>
    /// Day of week: 0 = Monday, 1 = Tuesday, ..., 6 = Sunday
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>
    /// If true, the service is closed on this day of the week
    /// </summary>
    public bool IsClosed { get; set; }

    // First shift (e.g., lunch)
    public TimeSpan? OpeningTime1 { get; set; }
    public TimeSpan? ClosingTime1 { get; set; }

    // Second shift (e.g., dinner) - optional
    public TimeSpan? OpeningTime2 { get; set; }
    public TimeSpan? ClosingTime2 { get; set; }

    /// <summary>
    /// Maximum capacity for this day (optional, can be overridden at slot level)
    /// </summary>
    public int? MaxCapacity { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
