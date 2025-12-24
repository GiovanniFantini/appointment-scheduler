using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Represents a specific time slot within an availability window
/// Used for BookingMode.TimeSlot services (e.g., restaurant reservations)
/// </summary>
public class AvailabilitySlot
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AvailabilityId { get; set; }

    /// <summary>
    /// The specific time for this slot (e.g., 12:00, 12:30, 13:00)
    /// </summary>
    [Required]
    [Column(TypeName = "time")]
    public TimeSpan SlotTime { get; set; }

    /// <summary>
    /// Maximum capacity for this specific slot (optional)
    /// If null, uses availability or service default
    /// </summary>
    public int? MaxCapacity { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AvailabilityId))]
    public Availability? Availability { get; set; }
}
