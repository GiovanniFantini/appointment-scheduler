namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Tipo di gestione dello straordinario
/// </summary>
public enum OvertimeType
{
    /// <summary>
    /// Straordinario da pagare
    /// </summary>
    Paid,

    /// <summary>
    /// Accumulato in banca ore
    /// </summary>
    BankedHours,

    /// <summary>
    /// Da recuperare con permessi
    /// </summary>
    TimeRecovery,

    /// <summary>
    /// Volontario (non richiede compenso)
    /// </summary>
    Voluntary,

    /// <summary>
    /// In attesa di decisione
    /// </summary>
    Pending
}
