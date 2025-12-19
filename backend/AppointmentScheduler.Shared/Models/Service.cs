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

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
