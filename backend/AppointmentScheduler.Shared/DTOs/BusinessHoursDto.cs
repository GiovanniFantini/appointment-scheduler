namespace AppointmentScheduler.Shared.DTOs;

public class BusinessHoursDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan? OpeningTime { get; set; }
    public TimeSpan? ClosingTime { get; set; }
    public TimeSpan? SecondOpeningTime { get; set; }
    public TimeSpan? SecondClosingTime { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBusinessHoursDto
{
    public int DayOfWeek { get; set; } // 0-6
    public TimeSpan? OpeningTime { get; set; }
    public TimeSpan? ClosingTime { get; set; }
    public TimeSpan? SecondOpeningTime { get; set; }
    public TimeSpan? SecondClosingTime { get; set; }
}

public class UpdateBusinessHoursDto
{
    public TimeSpan? OpeningTime { get; set; }
    public TimeSpan? ClosingTime { get; set; }
    public TimeSpan? SecondOpeningTime { get; set; }
    public TimeSpan? SecondClosingTime { get; set; }
    public bool? IsActive { get; set; }
}
