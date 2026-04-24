namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Titolari e co-titolari di un evento.
/// </summary>
public class EventParticipant
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int EmployeeId { get; set; }

    /// <summary>Se true, è il titolare principale dell'evento.</summary>
    public bool IsOwner { get; set; } = false;

    /// <summary>
    /// Override opzionale dell'orario di inizio per questo partecipante.
    /// Null = eredita Event.StartTime.
    /// </summary>
    public TimeOnly? StartTimeOverride { get; set; }

    /// <summary>
    /// Override opzionale dell'orario di fine per questo partecipante.
    /// Null = eredita Event.EndTime.
    /// </summary>
    public TimeOnly? EndTimeOverride { get; set; }

    /// <summary>Note specifiche per il singolo partecipante (es. "rientra dopo visita").</summary>
    public string? ParticipantNotes { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
