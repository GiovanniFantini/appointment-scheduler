using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.General;
    public bool IsRead { get; set; } = false;

    /// <summary>ID dell'entità correlata (es: EventId, RequestId).</summary>
    public int? RelatedEntityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
}
