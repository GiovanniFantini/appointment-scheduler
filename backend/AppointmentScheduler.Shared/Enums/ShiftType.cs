namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Tipo di turno
/// </summary>
public enum ShiftType
{
    Morning = 1,      // Mattina (es: 06:00-14:00)
    Afternoon = 2,    // Pomeriggio (es: 14:00-22:00)
    Evening = 3,      // Sera (es: 18:00-02:00)
    Night = 4,        // Notte (es: 22:00-06:00)
    FullDay = 5,      // Giornata completa
    Custom = 6        // Personalizzato
}
