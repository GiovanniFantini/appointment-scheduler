namespace AppointmentScheduler.Shared.Enums;

public enum HRDocumentStatus
{
    Draft = 1,      // Bozza (non visibile a dipendente)
    Published = 2,  // Pubblicato (visibile)
    Archived = 3    // Archiviato (read-only)
}
