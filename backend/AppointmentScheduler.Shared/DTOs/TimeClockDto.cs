using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Una singola timbratura restituita al client.
/// </summary>
public class TimeEntryDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public int EventParticipantId { get; set; }
    public string EventTitle { get; set; } = string.Empty;

    public TimeEntryType Type { get; set; }
    public string TypeName => Type.ToString();
    public TimeEntrySource Source { get; set; }
    public TimeEntryStatus Status { get; set; }
    public string StatusName => Status.ToString();

    public DateOnly WorkDate { get; set; }
    public DateTime ActualTimestampUtc { get; set; }
    public TimeOnly? ExpectedTime { get; set; }
    public int? DeviationMinutes { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? GpsAccuracyMeters { get; set; }
    public double? DistanceFromBranchMeters { get; set; }
    public bool? GeofenceOk { get; set; }

    public string? Notes { get; set; }
    public bool IsManualCorrection { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Payload inviato dal dipendente per timbrare (clock-in/out, break start/end).
/// Il tipo di timbratura è determinato dall'endpoint chiamato.
/// </summary>
public class ClockActionRequest
{
    /// <summary>
    /// Partecipazione al turno su cui timbrare. Opzionale: se omesso il
    /// servizio risolve automaticamente il turno corrente del dipendente.
    /// </summary>
    public int? EventParticipantId { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? GpsAccuracyMeters { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Stato corrente di timbratura del dipendente, usato dal widget Dashboard
/// e dalla pagina Timbratura.
/// </summary>
public class CurrentClockStatusDto
{
    /// <summary>True se la timbratura è attiva per il dipendente in questo merchant.</summary>
    public bool TimeClockEnabled { get; set; }

    public bool IsClockedIn { get; set; }
    public bool IsOnBreak { get; set; }

    public DateTime? ClockInAtUtc { get; set; }
    public DateTime? BreakStartAtUtc { get; set; }

    /// <summary>Turno corrente/imminente su cui timbrare, se presente.</summary>
    public TimeClockShiftDto? CurrentShift { get; set; }

    /// <summary>Minuti lavorati oggi sul turno corrente (pause escluse).</summary>
    public double WorkedMinutesToday { get; set; }

    /// <summary>Messaggio sintetico sullo stato (es. "In turno da 09:03").</summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>Azione suggerita al dipendente (es. "Timbra l'uscita").</summary>
    public string? SuggestedAction { get; set; }

    /// <summary>True se la filiale richiede la geolocalizzazione alla timbratura.</summary>
    public bool RequiresGeolocation { get; set; }

    /// <summary>
    /// True quando l'ora corrente è nell'intorno di un turno e ha senso
    /// proporre subito la timbratura in primo piano (widget Dashboard).
    /// </summary>
    public bool ShowClockPrompt { get; set; }

    /// <summary>Timbrature già registrate oggi sul turno corrente.</summary>
    public List<TimeEntryDto> TodayEntries { get; set; } = new();
}

/// <summary>
/// Riferimento sintetico a un turno, usato dentro CurrentClockStatusDto.
/// </summary>
public class TimeClockShiftDto
{
    public int EventId { get; set; }
    public int EventParticipantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
}

/// <summary>
/// Esito di un'azione di timbratura.
/// </summary>
public class ClockActionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeEntryDto Entry { get; set; } = null!;

    /// <summary>Stato aggiornato dopo l'azione.</summary>
    public CurrentClockStatusDto Status { get; set; } = null!;

    /// <summary>Anomalia generata dalla timbratura, se presente.</summary>
    public TimeClockAnomalyDto? Anomaly { get; set; }

    /// <summary>True se la timbratura ha generato un'anomalia.</summary>
    public bool HasAnomaly => Anomaly != null;

    /// <summary>Minuti lavorati sul turno (valorizzato dopo il clock-out).</summary>
    public double? WorkedShiftMinutes { get; set; }
}

/// <summary>
/// Configurazione della timbratura di una filiale.
/// </summary>
public class BranchTimeClockSettingsDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool ClockingRequired { get; set; }
    public int GraceInMinutes { get; set; }
    public int GraceOutMinutes { get; set; }
    public int EarlyClockInToleranceMinutes { get; set; }
    public int LateClockOutToleranceMinutes { get; set; }
    public bool GeofencingEnabled { get; set; }
    public int GeofenceRadiusMeters { get; set; }
    public bool BreakTrackingEnabled { get; set; }
    public int MaxBreakMinutes { get; set; }
    public int RoundingMinutes { get; set; }
    public bool RequirePhoto { get; set; }

    /// <summary>Coordinate della filiale (per il geofence). Null se non impostate.</summary>
    public double? BranchLatitude { get; set; }
    public double? BranchLongitude { get; set; }
}

/// <summary>
/// Payload per aggiornare la configurazione timbratura di una filiale.
/// </summary>
public class UpdateTimeClockSettingsRequest
{
    public bool IsEnabled { get; set; }
    public bool ClockingRequired { get; set; }
    public int GraceInMinutes { get; set; }
    public int GraceOutMinutes { get; set; }
    public int EarlyClockInToleranceMinutes { get; set; }
    public int LateClockOutToleranceMinutes { get; set; }
    public bool GeofencingEnabled { get; set; }
    public int GeofenceRadiusMeters { get; set; }
    public bool BreakTrackingEnabled { get; set; }
    public int MaxBreakMinutes { get; set; }
    public int RoundingMinutes { get; set; }
    public bool RequirePhoto { get; set; }

    /// <summary>
    /// Coordinate della filiale per il geofence. Se valorizzate vengono scritte
    /// su MerchantBranch; lasciarle null non modifica le coordinate esistenti.
    /// </summary>
    public double? BranchLatitude { get; set; }
    public double? BranchLongitude { get; set; }
}

/// <summary>
/// Payload per la correzione manuale di una timbratura da parte del merchant.
/// </summary>
public class CreateManualEntryRequest
{
    public int EventParticipantId { get; set; }
    public TimeEntryType Type { get; set; }
    public DateTime ActualTimestampUtc { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Anomalia di timbratura restituita al client.
/// </summary>
public class TimeClockAnomalyDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int? EventId { get; set; }
    public int? EventParticipantId { get; set; }
    public string? EventTitle { get; set; }
    public int? TimeEntryId { get; set; }

    public TimeClockAnomalyType Type { get; set; }
    public string TypeName => Type.ToString();
    public TimeClockAnomalyStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public int Severity { get; set; }

    public DateOnly WorkDate { get; set; }
    public int? DeviationMinutes { get; set; }
    public int? OvertimeMinutes { get; set; }

    public TimeClockAnomalyReason? EmployeeReason { get; set; }
    public string? EmployeeReasonName => EmployeeReason?.ToString();
    public string? EmployeeNotes { get; set; }
    public DateTime? JustifiedAt { get; set; }

    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Payload con cui il dipendente giustifica un'anomalia.</summary>
public class JustifyAnomalyRequest
{
    public TimeClockAnomalyReason Reason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Payload con cui il merchant approva/respinge un'anomalia.</summary>
public class ReviewAnomalyRequest
{
    public string? ReviewNotes { get; set; }
}

/// <summary>
/// Riga di report: ore lavorate da un dipendente in una giornata.
/// </summary>
public class TimeClockReportRowDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public string EventTitle { get; set; } = string.Empty;

    public DateTime? ClockInUtc { get; set; }
    public DateTime? ClockOutUtc { get; set; }

    /// <summary>Minuti effettivamente lavorati (pause escluse).</summary>
    public double WorkedMinutes { get; set; }

    /// <summary>Minuti di pausa totali.</summary>
    public double BreakMinutes { get; set; }

    /// <summary>Minuti pianificati per il turno.</summary>
    public double? ScheduledMinutes { get; set; }

    /// <summary>Minuti di straordinario (lavorato − pianificato, se positivo).</summary>
    public double OvertimeMinutes { get; set; }

    /// <summary>True se il turno ha almeno un'anomalia aperta o in revisione.</summary>
    public bool HasOpenAnomaly { get; set; }
}

/// <summary>
/// Statistiche di benessere del dipendente, calcolate dalle timbrature.
/// </summary>
public class WellbeingStatsDto
{
    public double WorkedMinutesThisWeek { get; set; }
    public double WorkedMinutesThisMonth { get; set; }
    public double OvertimeMinutesThisWeek { get; set; }
    public double OvertimeMinutesThisMonth { get; set; }

    /// <summary>Numero di anomalie ancora aperte da giustificare.</summary>
    public int OpenAnomalies { get; set; }

    /// <summary>Giorni lavorati consecutivi senza riposo.</summary>
    public int ConsecutiveWorkedDays { get; set; }

    /// <summary>True se è opportuno mostrare un avviso di carico eccessivo.</summary>
    public bool HasWellbeingAlert { get; set; }
    public string? WellbeingMessage { get; set; }
}
