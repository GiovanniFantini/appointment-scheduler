using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Anomalia rilevata su una timbratura o su un turno (ritardo, mancata
/// timbratura, fuori area, ...). Il workflow di giustificazione/revisione è
/// modellato su <see cref="EmployeeRequest"/>.
/// </summary>
public class TimeClockAnomaly
{
    public int Id { get; set; }

    public int MerchantId { get; set; }
    public int EmployeeId { get; set; }

    /// <summary>Turno coinvolto. Sempre valorizzato (le anomalie nascono da un turno).</summary>
    public int? EventId { get; set; }
    public int? EventParticipantId { get; set; }

    /// <summary>Timbratura che ha generato l'anomalia. Null per le mancate timbrature.</summary>
    public int? TimeEntryId { get; set; }

    public TimeClockAnomalyType Type { get; set; }
    public TimeClockAnomalyStatus Status { get; set; } = TimeClockAnomalyStatus.Open;

    /// <summary>Gravità 1 (lieve) – 3 (grave), per ordinamento e UI.</summary>
    public int Severity { get; set; } = 1;

    public DateOnly WorkDate { get; set; }

    /// <summary>Scarto in minuti rispetto all'atteso (per ritardi/anticipi).</summary>
    public int? DeviationMinutes { get; set; }

    /// <summary>Minuti di straordinario rilevati (per OvertimeDetected).</summary>
    public int? OvertimeMinutes { get; set; }

    // ── Giustificazione del dipendente ────────────────────────────────────
    public TimeClockAnomalyReason? EmployeeReason { get; set; }
    public string? EmployeeNotes { get; set; }
    public DateTime? JustifiedAt { get; set; }

    // ── Revisione del merchant ────────────────────────────────────────────
    public int? ReviewedByUserId { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public Event? Event { get; set; }
    public EventParticipant? EventParticipant { get; set; }
    public TimeEntry? TimeEntry { get; set; }
    public User? ReviewedBy { get; set; }
}
