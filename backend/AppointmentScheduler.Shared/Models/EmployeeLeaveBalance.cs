using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Saldo giorni di permesso/ferie disponibili per un dipendente
/// </summary>
public class EmployeeLeaveBalance
{
    public int Id { get; set; }

    /// <summary>
    /// Dipendente di riferimento
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Tipo di permesso/ferie
    /// </summary>
    public LeaveType LeaveType { get; set; }

    /// <summary>
    /// Anno di riferimento (es: 2026)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Giorni totali disponibili nell'anno
    /// </summary>
    public decimal TotalDays { get; set; }

    /// <summary>
    /// Giorni utilizzati
    /// </summary>
    public decimal UsedDays { get; set; }

    /// <summary>
    /// Giorni rimanenti (calcolato: TotalDays - UsedDays)
    /// </summary>
    public decimal RemainingDays => TotalDays - UsedDays;

    /// <summary>
    /// Note (es: "Include 2 giorni dall'anno precedente")
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
}
