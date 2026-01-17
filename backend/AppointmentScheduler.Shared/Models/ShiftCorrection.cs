namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Traccia correzioni auto-applicate dal dipendente entro 24h
/// </summary>
public class ShiftCorrection
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public int EmployeeId { get; set; }

    /// <summary>
    /// Campo corretto (CheckInTime, CheckOutTime, Break, etc.)
    /// </summary>
    public string CorrectedField { get; set; } = string.Empty;

    /// <summary>
    /// Valore originale (serializzato come JSON se complesso)
    /// </summary>
    public string? OriginalValue { get; set; }

    /// <summary>
    /// Nuovo valore
    /// </summary>
    public string NewValue { get; set; } = string.Empty;

    /// <summary>
    /// Motivo della correzione
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Se è stata applicata entro 24h (auto-approvata)
    /// </summary>
    public bool IsWithin24Hours { get; set; }

    /// <summary>
    /// Se richiede approvazione merchant (oltre 24h)
    /// </summary>
    public bool RequiresMerchantApproval { get; set; } = false;

    /// <summary>
    /// Se è stata approvata
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Note del merchant
    /// </summary>
    public string? MerchantNotes { get; set; }

    public DateTime CorrectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public Shift Shift { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
