namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Represents the standard operating hours for a merchant on a specific day of the week.
/// Supports split shifts (e.g., lunch 12:00-15:00, dinner 19:00-23:00).
/// </summary>
public class BusinessHours
{
    public int Id { get; set; }

    public int MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;

    /// <summary>
    /// Day of the week (0 = Sunday, 1 = Monday, ..., 6 = Saturday)
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>
    /// First opening time (e.g., 12:00 for lunch). Null means closed all day.
    /// </summary>
    public TimeSpan? OpeningTime { get; set; }

    /// <summary>
    /// First closing time (e.g., 15:00 for lunch)
    /// </summary>
    public TimeSpan? ClosingTime { get; set; }

    /// <summary>
    /// Second opening time for split shifts (e.g., 19:00 for dinner). Null means single shift.
    /// </summary>
    public TimeSpan? SecondOpeningTime { get; set; }

    /// <summary>
    /// Second closing time for split shifts (e.g., 23:00 for dinner)
    /// </summary>
    public TimeSpan? SecondClosingTime { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
