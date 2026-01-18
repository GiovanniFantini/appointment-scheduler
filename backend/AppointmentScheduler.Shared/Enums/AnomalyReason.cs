namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Motivo dell'anomalia - opzioni empatiche per il dipendente
/// </summary>
public enum AnomalyReason
{
    /// <summary>
    /// Traffico/Ritardi nei trasporti
    /// </summary>
    Traffic,

    /// <summary>
    /// Permesso autorizzato
    /// </summary>
    AuthorizedLeave,

    /// <summary>
    /// Recupero ore
    /// </summary>
    TimeRecovery,

    /// <summary>
    /// Emergenza personale
    /// </summary>
    PersonalEmergency,

    /// <summary>
    /// Dimenticanza (errore genuino)
    /// </summary>
    Forgotten,

    /// <summary>
    /// Problemi tecnici (app, dispositivo)
    /// </summary>
    TechnicalIssue,

    /// <summary>
    /// Smart working
    /// </summary>
    SmartWorking,

    /// <summary>
    /// Altro motivo (specificare in note)
    /// </summary>
    Other,

    /// <summary>
    /// Non specificato
    /// </summary>
    NotSpecified
}
