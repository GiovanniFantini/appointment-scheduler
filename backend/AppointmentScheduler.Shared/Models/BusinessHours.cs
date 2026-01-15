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

    /// <summary>
    /// Collection of shifts (opening/closing periods) for this day.
    /// Can have multiple shifts (e.g., breakfast 8-11, lunch 12-15, dinner 19-23)
    /// </summary>
    public ICollection<BusinessHoursShift> Shifts { get; set; } = new List<BusinessHoursShift>();

    /// <summary>
    /// Default maximum capacity for this day (can be overridden at shift level)
    /// </summary>
    public int? MaxCapacity { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
