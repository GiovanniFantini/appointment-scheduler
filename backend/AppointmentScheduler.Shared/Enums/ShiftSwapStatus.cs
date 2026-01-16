namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Stato della richiesta di scambio turno
/// </summary>
public enum ShiftSwapStatus
{
    Pending = 1,      // In attesa di risposta
    Approved = 2,     // Approvato
    Rejected = 3,     // Rifiutato
    Cancelled = 4     // Cancellato dal richiedente
}
