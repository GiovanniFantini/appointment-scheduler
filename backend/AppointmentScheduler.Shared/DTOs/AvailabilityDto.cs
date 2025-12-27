namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO for availability slot time
/// </summary>
public class AvailabilitySlotDto
{
    public int Id { get; set; }
    public TimeSpan SlotTime { get; set; }
    public int? MaxCapacity { get; set; }
}

/// <summary>
/// DTO for service availability
/// </summary>
public class AvailabilityDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int? DayOfWeek { get; set; }
    public DateTime? SpecificDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsRecurring { get; set; }
    public int? MaxCapacity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AvailabilitySlotDto> Slots { get; set; } = new();

    // Helper properties
    public string DayOfWeekName => DayOfWeek.HasValue
        ? ((DayOfWeek)DayOfWeek.Value).ToString()
        : string.Empty;
}

/// <summary>
/// Request to create a new availability
/// </summary>
public class CreateAvailabilityRequest
{
    public int ServiceId { get; set; }
    public int? DayOfWeek { get; set; }
    public DateTime? SpecificDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsRecurring { get; set; }
    public int? MaxCapacity { get; set; }
    public List<CreateAvailabilitySlotRequest>? Slots { get; set; }
}

/// <summary>
/// Request to create availability slots
/// </summary>
public class CreateAvailabilitySlotRequest
{
    public TimeSpan SlotTime { get; set; }
    public int? MaxCapacity { get; set; }
}

/// <summary>
/// Request to update an availability
/// </summary>
public class UpdateAvailabilityRequest
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int? MaxCapacity { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Response with available slots for a specific date
/// </summary>
public class AvailableSlotDto
{
    public DateTime Date { get; set; }
    public TimeSpan SlotTime { get; set; }
    public int AvailableCapacity { get; set; }
    public int TotalCapacity { get; set; }
    public bool IsAvailable => AvailableCapacity > 0;
}
