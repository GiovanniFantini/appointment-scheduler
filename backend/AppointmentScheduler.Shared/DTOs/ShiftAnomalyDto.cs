using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class ShiftAnomalyDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public AnomalyType Type { get; set; }
    public int Severity { get; set; }
    public string EmpatheticMessage { get; set; } = string.Empty;
    public AnomalyReason? EmployeeReason { get; set; }
    public string? EmployeeNotes { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolutionMethod { get; set; }
    public string? MerchantNotes { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool RequiresMerchantReview { get; set; }
}

public class ResolveAnomalyRequest
{
    public int AnomalyId { get; set; }
    public AnomalyReason Reason { get; set; }
    public string? Notes { get; set; }
}
