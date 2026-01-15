namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Represents an exception to the standard business hours for a specific date.
/// Used for holidays, special events, or temporary closures.
/// </summary>
public class BusinessHoursException
{
    public int Id { get; set; }

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    /// <summary>
    /// Specific date for this exception
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// If true, the service is closed on this specific date
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// Reason for the exception (e.g., "Christmas Holiday", "Summer Vacation", "Special Event")
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Collection of shifts for this exception date (if not closed).
    /// Allows defining custom hours for special days.
    /// </summary>
    public ICollection<BusinessHoursShift> Shifts { get; set; } = new List<BusinessHoursShift>();

    /// <summary>
    /// Maximum capacity for this specific date (can be overridden at shift level)
    /// </summary>
    public int? MaxCapacity { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
