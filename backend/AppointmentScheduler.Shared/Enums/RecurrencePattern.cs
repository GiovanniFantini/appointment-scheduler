namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Pattern di ricorrenza per template di turni
/// </summary>
public enum RecurrencePattern
{
    None = 0,         // Nessuna ricorrenza (turno singolo)
    Daily = 1,        // Ogni giorno
    Weekly = 2,       // Ogni settimana
    BiWeekly = 3,     // Ogni due settimane
    Monthly = 4       // Ogni mese
}
