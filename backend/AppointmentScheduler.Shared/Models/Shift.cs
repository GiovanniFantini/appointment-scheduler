using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Rappresenta un singolo turno in una data specifica
/// </summary>
public class Shift
{
    public int Id { get; set; }
    public int MerchantId { get; set; }

    /// <summary>
    /// FK al template (nullable se turno custom non basato su template)
    /// </summary>
    public int? ShiftTemplateId { get; set; }

    /// <summary>
    /// FK al dipendente assegnato (nullable se non ancora assegnato)
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Data del turno
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Ora di inizio
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Ora di fine
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Durata pausa in minuti
    /// </summary>
    public int BreakDurationMinutes { get; set; } = 0;

    /// <summary>
    /// Tipo di turno
    /// </summary>
    public ShiftType ShiftType { get; set; }

    /// <summary>
    /// Colore per visualizzazione
    /// </summary>
    public string Color { get; set; } = "#2196F3";

    /// <summary>
    /// Note aggiuntive per il turno
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Indica se il turno è stato confermato dal dipendente
    /// </summary>
    public bool IsConfirmed { get; set; } = false;

    /// <summary>
    /// Indica se il dipendente ha fatto check-in
    /// </summary>
    public bool IsCheckedIn { get; set; } = false;

    /// <summary>
    /// Timestamp check-in
    /// </summary>
    public DateTime? CheckInTime { get; set; }

    /// <summary>
    /// Indica se il dipendente ha fatto check-out
    /// </summary>
    public bool IsCheckedOut { get; set; } = false;

    /// <summary>
    /// Timestamp check-out
    /// </summary>
    public DateTime? CheckOutTime { get; set; }

    /// <summary>
    /// Stato di validazione del turno (auto-approvazione 95%)
    /// </summary>
    public ValidationStatus ValidationStatus { get; set; } = ValidationStatus.Pending;

    /// <summary>
    /// Timestamp validazione
    /// </summary>
    public DateTime? ValidatedAt { get; set; }

    /// <summary>
    /// Chi ha validato (MerchantId o "System" per auto-validazione)
    /// </summary>
    public string? ValidatedBy { get; set; }

    /// <summary>
    /// Posizione GPS al check-in (opzionale, formato JSON)
    /// </summary>
    public string? CheckInLocation { get; set; }

    /// <summary>
    /// Posizione GPS al check-out (opzionale, formato JSON)
    /// </summary>
    public string? CheckOutLocation { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public ShiftTemplate? ShiftTemplate { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<ShiftSwapRequest> SwapRequests { get; set; } = new List<ShiftSwapRequest>();
    public ICollection<ShiftBreak> Breaks { get; set; } = new List<ShiftBreak>();
    public ICollection<ShiftAnomaly> Anomalies { get; set; } = new List<ShiftAnomaly>();
    public ICollection<OvertimeRecord> OvertimeRecords { get; set; } = new List<OvertimeRecord>();
    public ICollection<ShiftCorrection> Corrections { get; set; } = new List<ShiftCorrection>();
}
