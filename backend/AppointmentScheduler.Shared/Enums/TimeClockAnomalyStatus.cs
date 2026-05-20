namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Stato del workflow di un'anomalia di timbratura.
/// </summary>
public enum TimeClockAnomalyStatus
{
    /// <summary>Rilevata, non ancora giustificata dal dipendente.</summary>
    Open = 1,

    /// <summary>Il dipendente ha fornito una giustificazione, in attesa di revisione.</summary>
    Justified = 2,

    /// <summary>Il merchant ha approvato la giustificazione.</summary>
    Approved = 3,

    /// <summary>Il merchant ha respinto la giustificazione.</summary>
    Rejected = 4
}
