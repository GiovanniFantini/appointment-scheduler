using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Represents a time period when a service is available for booking
/// Can be recurring (weekly pattern) or one-time (specific date)
/// </summary>
public class Availability
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ServiceId { get; set; }

    /// <summary>
    /// Day of week for recurring availabilities (0=Sunday, 6=Saturday)
    /// Null for one-time availabilities
    /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Specific date for one-time availabilities
    /// Null for recurring availabilities
    /// </summary>
    [Column(TypeName = "date")]
    public DateTime? SpecificDate { get; set; }

    /// <summary>
    /// Start time of availability window
    /// </summary>
    [Required]
    [Column(TypeName = "time")]
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of availability window
    /// </summary>
    [Required]
    [Column(TypeName = "time")]
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// True for weekly recurring pattern, false for specific date
    /// </summary>
    [Required]
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Maximum total capacity for this availability window (optional)
    /// If null, uses service default or unlimited
    /// </summary>
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Whether this availability is currently active
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ServiceId))]
    public Service? Service { get; set; }

    public ICollection<AvailabilitySlot> Slots { get; set; } = new List<AvailabilitySlot>();
}
