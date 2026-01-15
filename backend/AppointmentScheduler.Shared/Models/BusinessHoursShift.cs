namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Represents a single shift (opening/closing period) within a business day.
/// Multiple shifts can be defined for a single day (e.g., breakfast, lunch, dinner).
/// </summary>
public class BusinessHoursShift
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to BusinessHours (for standard weekly hours)
    /// </summary>
    public int? BusinessHoursId { get; set; }
    public BusinessHours? BusinessHours { get; set; }

    /// <summary>
    /// Foreign key to BusinessHoursException (for exception dates)
    /// </summary>
    public int? BusinessHoursExceptionId { get; set; }
    public BusinessHoursException? BusinessHoursException { get; set; }

    /// <summary>
    /// Opening time for this shift
    /// </summary>
    public TimeSpan OpeningTime { get; set; }

    /// <summary>
    /// Closing time for this shift
    /// </summary>
    public TimeSpan ClosingTime { get; set; }

    /// <summary>
    /// Optional label for this shift (e.g., "Breakfast", "Lunch", "Dinner")
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Maximum capacity for this specific shift (optional)
    /// </summary>
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Order/priority for displaying shifts (0 = first, 1 = second, etc.)
    /// </summary>
    public int SortOrder { get; set; }
}
