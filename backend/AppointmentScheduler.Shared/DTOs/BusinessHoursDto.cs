namespace AppointmentScheduler.Shared.DTOs;

public class BusinessHoursShiftDto
{
    public int Id { get; set; }
    public TimeSpan OpeningTime { get; set; }
    public TimeSpan ClosingTime { get; set; }
    public string? Label { get; set; }
    public int? MaxCapacity { get; set; }
    public int SortOrder { get; set; }
}

public class CreateBusinessHoursShiftDto
{
    public TimeSpan OpeningTime { get; set; }
    public TimeSpan ClosingTime { get; set; }
    public string? Label { get; set; }
    public int? MaxCapacity { get; set; }
    public int SortOrder { get; set; }
}

public class BusinessHoursDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public List<BusinessHoursShiftDto> Shifts { get; set; } = new();
    public int? MaxCapacity { get; set; }
}

public class CreateBusinessHoursDto
{
    public int ServiceId { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public List<CreateBusinessHoursShiftDto> Shifts { get; set; } = new();
    public int? MaxCapacity { get; set; }
}

public class UpdateBusinessHoursDto
{
    public bool IsClosed { get; set; }
    public List<CreateBusinessHoursShiftDto> Shifts { get; set; } = new();
    public int? MaxCapacity { get; set; }
}
