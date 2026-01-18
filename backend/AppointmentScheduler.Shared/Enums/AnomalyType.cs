namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Tipo di anomalia rilevata nel turno
/// </summary>
public enum AnomalyType
{
    /// <summary>
    /// Check-in in ritardo rispetto all'orario previsto
    /// </summary>
    LateCheckIn,

    /// <summary>
    /// Check-in in anticipo rispetto all'orario previsto
    /// </summary>
    EarlyCheckIn,

    /// <summary>
    /// Check-out in ritardo (straordinario)
    /// </summary>
    LateCheckOut,

    /// <summary>
    /// Check-out in anticipo
    /// </summary>
    EarlyCheckOut,

    /// <summary>
    /// Check-in dimenticato
    /// </summary>
    MissingCheckIn,

    /// <summary>
    /// Check-out dimenticato
    /// </summary>
    MissingCheckOut,

    /// <summary>
    /// Pausa più lunga del previsto
    /// </summary>
    ExtendedBreak,

    /// <summary>
    /// Pattern insolito rilevato
    /// </summary>
    UnusualPattern,

    /// <summary>
    /// Posizione GPS non corretta
    /// </summary>
    LocationMismatch
}
