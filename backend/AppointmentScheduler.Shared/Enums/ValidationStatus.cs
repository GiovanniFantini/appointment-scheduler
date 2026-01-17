namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Stato di validazione del turno
/// </summary>
public enum ValidationStatus
{
    /// <summary>
    /// In attesa di validazione
    /// </summary>
    Pending,

    /// <summary>
    /// Validato automaticamente (95% dei casi)
    /// </summary>
    AutoApproved,

    /// <summary>
    /// Validato manualmente dal merchant
    /// </summary>
    ManuallyApproved,

    /// <summary>
    /// Richiede attenzione del merchant
    /// </summary>
    RequiresReview,

    /// <summary>
    /// Rifiutato
    /// </summary>
    Rejected,

    /// <summary>
    /// Corretto dal dipendente (entro 24h)
    /// </summary>
    SelfCorrected
}
