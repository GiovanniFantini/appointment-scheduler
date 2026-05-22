namespace AppointmentScheduler.Core.Interfaces;

public interface IUtcClock
{
    DateTime UtcNow { get; }
}