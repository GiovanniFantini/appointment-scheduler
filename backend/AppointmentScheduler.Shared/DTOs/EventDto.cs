using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class EventDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public string EventTypeName => EventType.ToString();
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsOnCall { get; set; }
    public string? Recurrence { get; set; }
    public bool NotificationEnabled { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<EventParticipantDto> Participants { get; set; } = new();

    /// <summary>
    /// Avvisi non bloccanti emessi durante la mutation (create/update/clone):
    /// conflitti con ferie approvate, sovrapposizioni di turni, ecc.
    /// Vuoto nelle read.
    /// </summary>
    public List<ShiftConflictDto> Warnings { get; set; } = new();
}

public class EventParticipantDto
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public TimeOnly? StartTimeOverride { get; set; }
    public TimeOnly? EndTimeOverride { get; set; }
    public string? ParticipantNotes { get; set; }
}

/// <summary>
/// Override opzionali per un singolo partecipante già presente in OwnerEmployeeIds / CoOwnerEmployeeIds.
/// Serve a specificare orari individuali o note senza rompere il contratto esistente.
/// </summary>
public class ParticipantOverride
{
    public int EmployeeId { get; set; }
    public TimeOnly? StartTimeOverride { get; set; }
    public TimeOnly? EndTimeOverride { get; set; }
    public string? ParticipantNotes { get; set; }
}

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsAllDay { get; set; } = false;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsOnCall { get; set; } = false;
    public string? Recurrence { get; set; }
    public bool NotificationEnabled { get; set; } = false;
    public string? Notes { get; set; }
    public List<int> OwnerEmployeeIds { get; set; } = new();
    public List<int> CoOwnerEmployeeIds { get; set; } = new();
    public List<ParticipantOverride> ParticipantOverrides { get; set; } = new();
}

public class UpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsAllDay { get; set; } = false;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsOnCall { get; set; } = false;
    public string? Recurrence { get; set; }
    public bool NotificationEnabled { get; set; } = false;
    public string? Notes { get; set; }
    public List<int> OwnerEmployeeIds { get; set; } = new();
    public List<int> CoOwnerEmployeeIds { get; set; } = new();
    public List<ParticipantOverride> ParticipantOverrides { get; set; } = new();
}

public class CloneEventRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

/// <summary>
/// Clona tutti i turni di una settimana sorgente su una o più settimane target.
/// </summary>
public class CloneWeekRequest
{
    public DateOnly SourceWeekStart { get; set; }
    public DateOnly TargetWeekStart { get; set; }
    public int NumberOfWeeks { get; set; } = 1;
    /// <summary>Se valorizzato, clona solo i turni che hanno almeno uno di questi dipendenti come partecipante.</summary>
    public List<int>? EmployeeFilter { get; set; }
}

/// <summary>
/// Tipologia di conflitto rilevato durante l'assegnazione a un turno.
/// </summary>
public enum ShiftConflictKind
{
    LeaveOverlap = 1,
    ShiftOverlap = 2
}

/// <summary>
/// Avviso non bloccante: segnala che l'assegnazione di un dipendente a un turno
/// si sovrappone con ferie approvate o con un altro turno.
/// </summary>
public class ShiftConflictDto
{
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public ShiftConflictKind Kind { get; set; }
    public string KindName => Kind.ToString();

    /// <summary>Id della richiesta approvata in conflitto (solo per LeaveOverlap).</summary>
    public int? RequestId { get; set; }
    /// <summary>Tipo della richiesta (solo per LeaveOverlap).</summary>
    public EmployeeRequestType? RequestType { get; set; }

    /// <summary>Id dell'altro turno in conflitto (solo per ShiftOverlap).</summary>
    public int? ConflictingEventId { get; set; }
    public string? ConflictingEventTitle { get; set; }

    public TimeOnly? ConflictStart { get; set; }
    public TimeOnly? ConflictEnd { get; set; }

    public string Message { get; set; } = string.Empty;
}
