namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Represents a period when the merchant is closed (holidays, vacation, special events).
/// Overrides BusinessHours and Service Availabilities during the specified period.
/// </summary>
public class ClosurePeriod
{
    public int Id { get; set; }

    public int MerchantId { get; set; }
    public Merchant Merchant { get; set; } = null!;

    /// <summary>
    /// Start date of the closure (inclusive)
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the closure (inclusive)
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Reason for closure (e.g., "Summer vacation", "Christmas holidays", "Renovation")
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Optional description with more details
    /// </summary>
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
