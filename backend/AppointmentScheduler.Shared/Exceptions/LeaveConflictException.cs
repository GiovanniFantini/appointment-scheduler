using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Shared.Exceptions;

/// <summary>
/// Eccezione lanciata quando un turno va in conflitto con ferie/permessi approvati o in attesa
/// </summary>
public class LeaveConflictException : Exception
{
    public List<LeaveConflictInfo> Conflicts { get; }

    public LeaveConflictException(string message, List<LeaveConflictInfo> conflicts)
        : base(message)
    {
        Conflicts = conflicts;
    }
}
