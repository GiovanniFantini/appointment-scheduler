namespace AppointmentScheduler.Shared.Enums;

public enum UserRole
{
    User = 1,      // Consumer - può solo prenotare
    Merchant = 2,  // Business owner - gestisce le proprie attività
    Admin = 3      // Admin - gestisce permessi e approva merchant
}
