namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Tipologie di permessi e assenze
/// </summary>
public enum LeaveType
{
    Ferie = 1,           // Ferie annuali
    ROL = 2,             // Riduzione Orario di Lavoro
    PAR = 3,             // Permessi Annuali Retribuiti
    Malattia = 4,        // Malattia
    ExFestivita = 5,     // Ex-festività
    Welfare = 6,         // Welfare aziendale
    Permesso = 7,        // Permesso generico
    Altro = 8            // Altro tipo di assenza
}
