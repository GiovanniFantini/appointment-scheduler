using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class Service
{
    public int Id { get; set; }
    public int MerchantId { get; set; } // FK a Merchant
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceType ServiceType { get; set; }
    public decimal? Price { get; set; }
    public int DurationMinutes { get; set; } // Durata del servizio
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Configurazione specifiche per verticale (JSON flessibile)
    public string? Configuration { get; set; } // Es: {"capacity": 4, "court": "Tennis"}

    // Phase 2: Booking mode configuration
    public BookingMode BookingMode { get; set; } = BookingMode.TimeSlot;

    /// <summary>
    /// For TimeSlot mode: duration of each slot in minutes
    /// If null, uses DurationMinutes
    /// </summary>
    public int? SlotDurationMinutes { get; set; }

    /// <summary>
    /// Maximum capacity per slot/time period
    /// If null, unlimited or managed at availability level
    /// </summary>
    public int? MaxCapacityPerSlot { get; set; }

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
    public ICollection<BusinessHours> BusinessHours { get; set; } = new List<BusinessHours>();
    public ICollection<BusinessHoursException> BusinessHoursExceptions { get; set; } = new List<BusinessHoursException>();
}
