namespace AppointmentScheduler.Shared.Enums;

public enum BookingStatus
{
    Pending = 1,      // In attesa di conferma
    Confirmed = 2,    // Confermata dal merchant
    Cancelled = 3,    // Cancellata
    Completed = 4,    // Completata
    NoShow = 5        // Cliente non si è presentato
}
