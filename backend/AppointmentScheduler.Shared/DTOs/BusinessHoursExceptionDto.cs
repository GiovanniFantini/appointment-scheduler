namespace AppointmentScheduler.Shared.DTOs;

public class BusinessHoursExceptionDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public DateTime Date { get; set; }
    public bool IsClosed { get; set; }
    public string? Reason { get; set; }
    public List<BusinessHoursShiftDto> Shifts { get; set; } = new();
    public int? MaxCapacity { get; set; }
}

public class CreateBusinessHoursExceptionDto
{
    public int ServiceId { get; set; }
    public DateTime Date { get; set; }
    public bool IsClosed { get; set; }
    public string? Reason { get; set; }
    public List<CreateBusinessHoursShiftDto> Shifts { get; set; } = new();
    public int? MaxCapacity { get; set; }
}

public class UpdateBusinessHoursExceptionDto
{
    public bool IsClosed { get; set; }
    public string? Reason { get; set; }
    public List<CreateBusinessHoursShiftDto> Shifts { get; set; } = new();
    public int? MaxCapacity { get; set; }
}
