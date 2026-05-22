using AppointmentScheduler.Core.Interfaces;

namespace AppointmentScheduler.Core.Services;

public sealed class SystemWallClock : IWallClock
{
    public DateTime Now => DateTime.Now;
}