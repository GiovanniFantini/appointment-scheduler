namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Traccia la lettura di un messaggio della bacheca da parte di un dipendente
/// </summary>
public class BoardMessageRead
{
    public int Id { get; set; }
    public int BoardMessageId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public BoardMessage BoardMessage { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
