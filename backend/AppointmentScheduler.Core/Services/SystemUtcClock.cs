using AppointmentScheduler.Core.Interfaces;

namespace AppointmentScheduler.Core.Services;

public sealed class SystemUtcClock : IUtcClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}