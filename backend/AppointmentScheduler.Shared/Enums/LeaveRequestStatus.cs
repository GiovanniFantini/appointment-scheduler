namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Stato della richiesta di permesso/ferie
/// </summary>
public enum LeaveRequestStatus
{
    Pending = 1,      // In attesa di approvazione
    Approved = 2,     // Approvata
    Rejected = 3,     // Rifiutata
    Cancelled = 4     // Cancellata dal dipendente
}
