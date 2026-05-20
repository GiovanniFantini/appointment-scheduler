namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Tipo di anomalia rilevata su una timbratura o su un turno.
/// </summary>
public enum TimeClockAnomalyType
{
    LateClockIn = 1,
    EarlyClockIn = 2,
    LateClockOut = 3,
    EarlyClockOut = 4,
    MissingClockIn = 5,
    MissingClockOut = 6,
    ExtendedBreak = 7,
    LocationMismatch = 8,
    OvertimeDetected = 9
}
