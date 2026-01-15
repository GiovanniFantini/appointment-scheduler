namespace AppointmentScheduler.Shared.DTOs;

public class ClosurePeriodDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateClosurePeriodDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateClosurePeriodDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Reason { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
