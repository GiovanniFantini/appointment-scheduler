using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione/aggiornamento del saldo ferie di un dipendente
/// </summary>
public class UpsertEmployeeLeaveBalanceRequest
{
    public int EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO per la risposta con i dati del saldo ferie
/// </summary>
public class EmployeeLeaveBalanceDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public LeaveType LeaveType { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
