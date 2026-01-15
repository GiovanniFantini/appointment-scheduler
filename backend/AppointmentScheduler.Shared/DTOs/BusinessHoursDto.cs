namespace AppointmentScheduler.Shared.DTOs;

public class BusinessHoursDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public TimeSpan? OpeningTime1 { get; set; }
    public TimeSpan? ClosingTime1 { get; set; }
    public TimeSpan? OpeningTime2 { get; set; }
    public TimeSpan? ClosingTime2 { get; set; }
    public int? MaxCapacity { get; set; }
}

public class CreateBusinessHoursDto
{
    public int ServiceId { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public TimeSpan? OpeningTime1 { get; set; }
    public TimeSpan? ClosingTime1 { get; set; }
    public TimeSpan? OpeningTime2 { get; set; }
    public TimeSpan? ClosingTime2 { get; set; }
    public int? MaxCapacity { get; set; }
}

public class UpdateBusinessHoursDto
{
    public bool IsClosed { get; set; }
    public TimeSpan? OpeningTime1 { get; set; }
    public TimeSpan? ClosingTime1 { get; set; }
    public TimeSpan? OpeningTime2 { get; set; }
    public TimeSpan? ClosingTime2 { get; set; }
    public int? MaxCapacity { get; set; }
}
