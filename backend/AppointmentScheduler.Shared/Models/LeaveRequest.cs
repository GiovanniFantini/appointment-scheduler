using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Richiesta di permesso/ferie da parte di un dipendente
/// </summary>
public class LeaveRequest
{
    public int Id { get; set; }

    /// <summary>
    /// Dipendente che richiede il permesso
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Merchant di riferimento
    /// </summary>
    public int MerchantId { get; set; }

    /// <summary>
    /// Tipo di permesso/ferie
    /// </summary>
    public LeaveType LeaveType { get; set; }

    /// <summary>
    /// Data inizio assenza
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Data fine assenza
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Numero di giorni richiesti (calcolato automaticamente)
    /// </summary>
    public decimal DaysRequested { get; set; }

    /// <summary>
    /// Stato della richiesta
    /// </summary>
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    /// <summary>
    /// Note/motivazione del dipendente
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Risposta/note del merchant
    /// </summary>
    public string? ResponseNotes { get; set; }

    /// <summary>
    /// ID del merchant che ha approvato/rifiutato
    /// </summary>
    public int? ApprovedByMerchantId { get; set; }

    /// <summary>
    /// Timestamp approvazione/rifiuto
    /// </summary>
    public DateTime? ResponseAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Merchant Merchant { get; set; } = null!;
}
