namespace AppointmentScheduler.Shared.DTOs;

public class BusinessHoursExceptionDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public DateTime Date { get; set; }
    public bool IsClosed { get; set; }
    public string? Reason { get; set; }
    public TimeSpan? OpeningTime1 { get; set; }
    public TimeSpan? ClosingTime1 { get; set; }
    public TimeSpan? OpeningTime2 { get; set; }
    public TimeSpan? ClosingTime2 { get; set; }
    public int? MaxCapacity { get; set; }
}

public class CreateBusinessHoursExceptionDto
{
    public int ServiceId { get; set; }
    public DateTime Date { get; set; }
    public bool IsClosed { get; set; }
    public string? Reason { get; set; }
    public TimeSpan? OpeningTime1 { get; set; }
    public TimeSpan? ClosingTime1 { get; set; }
    public TimeSpan? OpeningTime2 { get; set; }
    public TimeSpan? ClosingTime2 { get; set; }
    public int? MaxCapacity { get; set; }
}

public class UpdateBusinessHoursExceptionDto
{
    public bool IsClosed { get; set; }
    public string? Reason { get; set; }
    public TimeSpan? OpeningTime1 { get; set; }
    public TimeSpan? ClosingTime1 { get; set; }
    public TimeSpan? OpeningTime2 { get; set; }
    public TimeSpan? ClosingTime2 { get; set; }
    public int? MaxCapacity { get; set; }
}
