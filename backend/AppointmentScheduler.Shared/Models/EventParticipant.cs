namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Titolari e co-titolari di un evento.
/// </summary>
public class EventParticipant
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int EmployeeId { get; set; }

    /// <summary>Se true, è il titolare principale dell'evento.</summary>
    public bool IsOwner { get; set; } = false;

    // Navigation properties
    public Event Event { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
