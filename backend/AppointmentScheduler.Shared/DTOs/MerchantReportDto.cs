namespace AppointmentScheduler.Shared.DTOs;

public class MerchantReportDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int ShiftCount { get; set; }
    public int AssignedShiftCount { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public List<MerchantReportDayDto> Days { get; set; } = [];
    public List<TimeClockReportRowDto> TimeEntries { get; set; } = [];
}

public class MerchantReportDayDto
{
    public DateOnly Date { get; set; }
    public int ShiftCount { get; set; }
    public int AssignedShiftCount { get; set; }
}
