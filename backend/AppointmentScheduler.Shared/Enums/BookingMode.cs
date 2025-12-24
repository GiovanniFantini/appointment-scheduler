namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Defines how customers can book a service
/// </summary>
public enum BookingMode
{
    /// <summary>
    /// Fixed time slots (e.g., restaurant reservations at 12:00, 12:30, 13:00)
    /// Customer selects from available slots
    /// </summary>
    TimeSlot = 1,

    /// <summary>
    /// Flexible time range within business hours (e.g., library, coworking space)
    /// Customer can specify start and/or end time within operating hours
    /// </summary>
    TimeRange = 2,

    /// <summary>
    /// Day-only booking without specific time (e.g., nightclub event, all-day pass)
    /// Customer only selects the date
    /// </summary>
    DayOnly = 3
}
