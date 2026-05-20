namespace AppointmentScheduler.Shared.Enums;

/// <summary>
/// Motivazione fornita dal dipendente nel giustificare un'anomalia.
/// </summary>
public enum TimeClockAnomalyReason
{
    NotSpecified = 0,
    Traffic = 1,
    AuthorizedLeave = 2,
    TimeRecovery = 3,
    PersonalEmergency = 4,
    Forgotten = 5,
    TechnicalIssue = 6,
    SmartWorking = 7,
    Other = 8
}
