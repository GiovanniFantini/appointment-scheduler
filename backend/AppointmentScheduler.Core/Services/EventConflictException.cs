using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

public sealed class EventConflictException : InvalidOperationException
{
    public EventConflictException(string message, IReadOnlyList<ShiftConflictDto> conflicts)
        : base(message)
    {
        Conflicts = conflicts.ToList();
    }

    public IReadOnlyList<ShiftConflictDto> Conflicts { get; }
}