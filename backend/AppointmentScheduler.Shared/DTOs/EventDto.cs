using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public enum CoverageStatus
{
    /// <summary>Nessun fabbisogno definito o nessun partecipante.</summary>
    None = 0,
    /// <summary>Fabbisogno presente ma nessun partecipante con la mansione richiesta.</summary>
    Empty = 1,
    /// <summary>Almeno una mansione richiesta non è coperta dalla quantità necessaria.</summary>
    Partial = 2,
    /// <summary>Tutte le mansioni richieste sono coperte.</summary>
    Covered = 3
}

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

    /// <summary>Fabbisogno mansioni per il turno (lista vuota se non definito).</summary>
    public List<EventRequiredSkillDto> RequiredSkills { get; set; } = new();

    /// <summary>Stato copertura calcolato lato server in base a RequiredSkills e Participants.SkillId.</summary>
    public CoverageStatus CoverageStatus { get; set; } = CoverageStatus.None;
    public string CoverageStatusName => CoverageStatus.ToString();

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

    /// <summary>Mansione con cui il dipendente partecipa al turno (opzionale).</summary>
    public int? SkillId { get; set; }
    public string? SkillName { get; set; }
    public string? SkillColor { get; set; }
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
    public List<EventRequiredSkillInput> RequiredSkills { get; set; } = new();
    public List<ParticipantSkillAssignment> ParticipantSkills { get; set; } = new();
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
    public List<EventRequiredSkillInput> RequiredSkills { get; set; } = new();
    public List<ParticipantSkillAssignment> ParticipantSkills { get; set; } = new();
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
    ShiftOverlap = 2,
    SkillMismatch = 3
}

/// <summary>
/// Avviso non bloccante: segnala che l'assegnazione di un dipendente a un turno
/// si sovrappone con ferie approvate, con un altro turno, oppure che la mansione
/// con cui partecipa non è tra quelle dichiarate sull'employee.
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

    /// <summary>Mansione richiesta non posseduta dal dipendente (solo per SkillMismatch).</summary>
    public int? SkillId { get; set; }
    public string? SkillName { get; set; }

    public string Message { get; set; } = string.Empty;
}
