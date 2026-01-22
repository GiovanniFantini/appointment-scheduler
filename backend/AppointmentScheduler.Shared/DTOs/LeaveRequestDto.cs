using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione di una richiesta di permesso/ferie
/// </summary>
public class CreateLeaveRequest
{
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO per rispondere a una richiesta di permesso/ferie
/// </summary>
public class RespondToLeaveRequest
{
    public LeaveRequestStatus Status { get; set; }
    public string? ResponseNotes { get; set; }
}

/// <summary>
/// DTO per la risposta con i dati della richiesta di permesso/ferie
/// </summary>
public class LeaveRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int MerchantId { get; set; }
    public LeaveType LeaveType { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DaysRequested { get; set; }
    public LeaveRequestStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ResponseNotes { get; set; }
    public int? ApprovedByMerchantId { get; set; }
    public DateTime? ResponseAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
