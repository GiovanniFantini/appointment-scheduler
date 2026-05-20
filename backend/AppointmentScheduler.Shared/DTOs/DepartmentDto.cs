namespace AppointmentScheduler.Shared.DTOs;

public class DepartmentDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
}

public class UpdateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
    public bool IsActive { get; set; } = true;
}
