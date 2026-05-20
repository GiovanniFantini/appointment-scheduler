namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Tipo di evento di timbratura. Una pausa è una coppia BreakStart/BreakEnd.
/// </summary>
public enum TimeEntryType
{
    ClockIn = 1,
    ClockOut = 2,
    BreakStart = 3,
    BreakEnd = 4
}
