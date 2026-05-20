using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Singolo evento di timbratura di un dipendente (log append-only).
/// Una giornata di lavoro è una sequenza di TimeEntry sullo stesso turno:
/// ClockIn → [BreakStart → BreakEnd]* → ClockOut.
/// Le timbrature esistono sempre su un turno pianificato (EventParticipant).
/// </summary>
public class TimeEntry
{
    public int Id { get; set; }

    // Denormalizzati per scoping multi-tenant e query veloci.
    public int MerchantId { get; set; }
    public int BranchId { get; set; }
    public int EmployeeId { get; set; }

    /// <summary>Turno a cui la timbratura si riferisce.</summary>
    public int EventId { get; set; }

    /// <summary>Partecipazione del dipendente al turno. Sempre valorizzata.</summary>
    public int EventParticipantId { get; set; }

    public TimeEntryType Type { get; set; }
    public TimeEntrySource Source { get; set; } = TimeEntrySource.Web;
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Ok;

    /// <summary>
    /// Giornata logica di lavoro: coincide con la data di inizio del turno.
    /// Gestisce i turni a cavallo di mezzanotte (tutte le timbrature di un
    /// turno notturno hanno la stessa WorkDate).
    /// </summary>
    public DateOnly WorkDate { get; set; }

    /// <summary>Istante effettivo della timbratura (UTC).</summary>
    public DateTime ActualTimestampUtc { get; set; }

    /// <summary>
    /// Orario atteso per questa timbratura, derivato dal turno (con eventuale
    /// override del partecipante). Per ClockIn = inizio turno, per ClockOut =
    /// fine turno. Null per le pause.
    /// </summary>
    public TimeOnly? ExpectedTime { get; set; }

    /// <summary>
    /// Scarto in minuti rispetto all'orario atteso (firmato: negativo = in
    /// anticipo, positivo = in ritardo). Null per le pause.
    /// </summary>
    public int? DeviationMinutes { get; set; }

    // ── Geolocalizzazione (Fase 2) ────────────────────────────────────────
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Accuratezza GPS dichiarata dal device, in metri.</summary>
    public double? GpsAccuracyMeters { get; set; }

    /// <summary>Distanza calcolata dalla filiale, in metri.</summary>
    public double? DistanceFromBranchMeters { get; set; }

    /// <summary>
    /// Esito del controllo geofence: true = entro il raggio, false = fuori
    /// area, null = geofence non valutato (disabilitato o coordinate assenti).
    /// </summary>
    public bool? GeofenceOk { get; set; }

    public string? Notes { get; set; }

    // ── Correzione manuale (merchant) ─────────────────────────────────────
    public bool IsManualCorrection { get; set; } = false;
    public int? CorrectedByUserId { get; set; }
    public DateTime? CorrectedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public MerchantBranch Branch { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public EventParticipant EventParticipant { get; set; } = null!;
    public User? CorrectedBy { get; set; }
}
