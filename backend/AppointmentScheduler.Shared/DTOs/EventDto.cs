using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class EventDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public string EventTypeName => EventType.ToString();
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsOnCall { get; set; }
    public string? Recurrence { get; set; }
    public bool NotificationEnabled { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<EventParticipantDto> Participants { get; set; } = new();
}

public class EventParticipantDto
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
}

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsAllDay { get; set; } = false;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsOnCall { get; set; } = false;
    public string? Recurrence { get; set; }
    public bool NotificationEnabled { get; set; } = false;
    public string? Notes { get; set; }
    public List<int> OwnerEmployeeIds { get; set; } = new();
    public List<int> CoOwnerEmployeeIds { get; set; } = new();
}

public class UpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsAllDay { get; set; } = false;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsOnCall { get; set; } = false;
    public string? Recurrence { get; set; }
    public bool NotificationEnabled { get; set; } = false;
    public string? Notes { get; set; }
    public List<int> OwnerEmployeeIds { get; set; } = new();
    public List<int> CoOwnerEmployeeIds { get; set; } = new();
}

public class CloneEventRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}
