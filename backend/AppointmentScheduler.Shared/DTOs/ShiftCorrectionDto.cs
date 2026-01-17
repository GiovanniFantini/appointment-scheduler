namespace AppointmentScheduler.Shared.DTOs;

public class ShiftCorrectionDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public int EmployeeId { get; set; }
    public string CorrectedField { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string NewValue { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool IsWithin24Hours { get; set; }
    public bool RequiresMerchantApproval { get; set; }
    public bool IsApproved { get; set; }
    public string? MerchantNotes { get; set; }
    public DateTime CorrectedAt { get; set; }
}

public class CorrectShiftRequest
{
    public int ShiftId { get; set; }
    public string CorrectedField { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class ApproveCorrectionRequest
{
    public int CorrectionId { get; set; }
    public bool Approve { get; set; }
    public string? Notes { get; set; }
}
