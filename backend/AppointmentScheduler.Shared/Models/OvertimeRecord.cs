using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Traccia straordinari con classificazione e gestione
/// </summary>
public class OvertimeRecord
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public int EmployeeId { get; set; }
    public int MerchantId { get; set; }

    /// <summary>
    /// Durata straordinario in minuti
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Tipo di gestione straordinario
    /// </summary>
    public OvertimeType Type { get; set; } = OvertimeType.Pending;

    /// <summary>
    /// Se lo straordinario è stato rilevato automaticamente
    /// </summary>
    public bool IsAutoDetected { get; set; } = true;

    /// <summary>
    /// Se lo straordinario è stato approvato
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Chi ha approvato (EmployeeId o "System" per auto-approvazione)
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Data approvazione
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Note dipendente
    /// </summary>
    public string? EmployeeNotes { get; set; }

    /// <summary>
    /// Note merchant
    /// </summary>
    public string? MerchantNotes { get; set; }

    /// <summary>
    /// Data straordinario
    /// </summary>
    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Shift Shift { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public Merchant Merchant { get; set; } = null!;
}
