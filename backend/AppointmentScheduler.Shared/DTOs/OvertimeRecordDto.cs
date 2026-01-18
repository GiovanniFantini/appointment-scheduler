using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class OvertimeRecordDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public int EmployeeId { get; set; }
    public int MerchantId { get; set; }
    public int DurationMinutes { get; set; }
    public OvertimeType Type { get; set; }
    public bool IsAutoDetected { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? EmployeeNotes { get; set; }
    public string? MerchantNotes { get; set; }
    public DateTime Date { get; set; }
}

public class ClassifyOvertimeRequest
{
    public int OvertimeId { get; set; }
    public OvertimeType Type { get; set; }
    public string? Notes { get; set; }
}

public class ApproveOvertimeRequest
{
    public int OvertimeId { get; set; }
    public bool Approve { get; set; }
    public string? Notes { get; set; }
}
