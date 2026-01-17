namespace AppointmentScheduler.Shared.DTOs;

public class ShiftBreakDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public DateTime BreakStartTime { get; set; }
    public DateTime? BreakEndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string BreakType { get; set; } = "General";
    public bool IsAutoSuggested { get; set; }
    public string? Notes { get; set; }
    public bool IsShortBreak { get; set; }
}

public class StartBreakRequest
{
    public int ShiftId { get; set; }
    public string BreakType { get; set; } = "General";
    public string? Notes { get; set; }
}

public class EndBreakRequest
{
    public int BreakId { get; set; }
    public string? Notes { get; set; }
}
