using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Richiesta di scambio turno tra dipendenti
/// </summary>
public class ShiftSwapRequest
{
    public int Id { get; set; }

    /// <summary>
    /// Turno che si vuole scambiare
    /// </summary>
    public int ShiftId { get; set; }

    /// <summary>
    /// Dipendente che richiede lo scambio
    /// </summary>
    public int RequestingEmployeeId { get; set; }

    /// <summary>
    /// Dipendente con cui si vuole scambiare (nullable se aperto a tutti)
    /// </summary>
    public int? TargetEmployeeId { get; set; }

    /// <summary>
    /// Turno offerto in cambio (nullable se non è uno scambio 1:1)
    /// </summary>
    public int? OfferedShiftId { get; set; }

    /// <summary>
    /// Stato della richiesta
    /// </summary>
    public ShiftSwapStatus Status { get; set; } = ShiftSwapStatus.Pending;

    /// <summary>
    /// Messaggio/motivazione della richiesta
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Risposta dal dipendente target o dal merchant
    /// </summary>
    public string? ResponseMessage { get; set; }

    /// <summary>
    /// Indica se la richiesta richiede approvazione del merchant
    /// </summary>
    public bool RequiresMerchantApproval { get; set; } = true;

    /// <summary>
    /// Timestamp approvazione/rifiuto merchant
    /// </summary>
    public DateTime? MerchantResponseAt { get; set; }

    /// <summary>
    /// ID del merchant che ha approvato/rifiutato
    /// </summary>
    public int? ApprovedByMerchantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Shift Shift { get; set; } = null!;
    public Employee RequestingEmployee { get; set; } = null!;
    public Employee? TargetEmployee { get; set; }
    public Shift? OfferedShift { get; set; }
}
