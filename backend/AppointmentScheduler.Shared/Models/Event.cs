using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Entità centrale del sistema. Rappresenta qualsiasi evento aziendale:
/// turni, ferie, permessi, malattia, chiusure.
/// </summary>
public class Event
{
    public int Id { get; set; }
    public int MerchantId { get; set; }

    public string Title { get; set; } = string.Empty;
    public EventType EventType { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public bool IsAllDay { get; set; } = false;

    /// <summary>Ora di inizio (precisione al minuto). Null se IsAllDay = true.</summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>Ora di fine (precisione al minuto). Null se IsAllDay = true.</summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// Flag reperibilità / on-call. Applicabile solo a EventType.Turno.
    /// </summary>
    public bool IsOnCall { get; set; } = false;

    /// <summary>
    /// Regola di ricorrenza in formato crontab-like.
    /// Null = evento singolo. Es: "WEEKLY;BYDAY=MO,WE,FR"
    /// </summary>
    public string? Recurrence { get; set; }

    public bool NotificationEnabled { get; set; } = false;
    public string? Notes { get; set; }

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
}
