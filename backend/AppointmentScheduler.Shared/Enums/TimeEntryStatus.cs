namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Stato di una timbratura.
/// </summary>
public enum TimeEntryStatus
{
    /// <summary>Timbratura regolare, entro le tolleranze.</summary>
    Ok = 1,

    /// <summary>Timbratura con un'anomalia associata (ritardo, fuori area, ...).</summary>
    Anomaly = 2,

    /// <summary>Timbratura modificata da una correzione manuale del merchant.</summary>
    Corrected = 3,

    /// <summary>Anomalia in attesa di revisione da parte del merchant.</summary>
    PendingReview = 4
}
