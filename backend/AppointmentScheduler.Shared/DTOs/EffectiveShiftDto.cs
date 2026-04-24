using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Rappresenta il "turno effettivo" di un dipendente in una data: turno canonico
/// con eventuali permessi approvati sottratti, risultante in una lista di segmenti di presenza.
/// </summary>
public class EffectiveShiftDto
{
    public int EventId { get; set; }
    public int EmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public bool IsAllDay { get; set; }

    /// <summary>Orario canonico di inizio (dopo eventuale override partecipante).</summary>
    public TimeOnly? CanonicalStart { get; set; }

    /// <summary>Orario canonico di fine (dopo eventuale override partecipante).</summary>
    public TimeOnly? CanonicalEnd { get; set; }

    /// <summary>
    /// Segmenti di presenza residui dopo la sottrazione dei permessi.
    /// Vuoto se IsFullyAbsent = true. Per turni all-day con nessun permesso, contiene un solo segmento null-null.
    /// </summary>
    public List<PresenceSegment> Segments { get; set; } = new();

    /// <summary>Elenco dei permessi applicati (per UI/tooltip).</summary>
    public List<AppliedLeaveDto> AppliedLeaves { get; set; } = new();

    /// <summary>True se tutti i segmenti sono stati coperti da permessi.</summary>
    public bool IsFullyAbsent { get; set; }

    public bool IsOnCall { get; set; }
}

public class PresenceSegment
{
    public TimeOnly? From { get; set; }
    public TimeOnly? To { get; set; }
}

public class AppliedLeaveDto
{
    public int RequestId { get; set; }
    public EmployeeRequestType Type { get; set; }
    public string TypeName => Type.ToString();
    public bool IsFullDay { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Notes { get; set; }
}
