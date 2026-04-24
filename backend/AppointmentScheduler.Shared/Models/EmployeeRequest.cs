using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class EmployeeRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public int MerchantId { get; set; }

    public EmployeeRequestType Type { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Ora di inizio per permessi orari. Null = richiesta "tutto il giorno".
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// Ora di fine per permessi orari. Null = richiesta "tutto il giorno".
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// Turno specifico a cui la richiesta è collegata (opzionale).
    /// Se null, la richiesta si applica a tutti i turni del dipendente nelle date coperte.
    /// </summary>
    public int? EventId { get; set; }

    public string? Notes { get; set; }
    public string? ReviewNotes { get; set; }

    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Merchant Merchant { get; set; } = null!;
    public User? ReviewedBy { get; set; }
    public Event? Event { get; set; }
}
