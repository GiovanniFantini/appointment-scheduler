namespace AppointmentScheduler.Core.Interfaces;

public interface IWallClock
{
    DateTime Now { get; }
}