using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione di un turno
/// </summary>
public class CreateShiftRequest
{
    public int? ShiftTemplateId { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDurationMinutes { get; set; } = 0;
    public ShiftType ShiftType { get; set; }
    public string Color { get; set; } = "#2196F3";
    public string? Notes { get; set; }
}

/// <summary>
/// DTO per la creazione multipla di turni da template
/// </summary>
public class CreateShiftsFromTemplateRequest
{
    public int ShiftTemplateId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? EmployeeId { get; set; }
    public List<DayOfWeek>? DaysOfWeek { get; set; }
}

/// <summary>
/// DTO per l'aggiornamento di un turno
/// </summary>
public class UpdateShiftRequest
{
    public int? EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDurationMinutes { get; set; } = 0;
    public ShiftType ShiftType { get; set; }
    public string Color { get; set; } = "#2196F3";
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO per assegnare un turno a un dipendente
/// </summary>
public class AssignShiftRequest
{
    public int EmployeeId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO per la risposta con i dati del turno
/// </summary>
public class ShiftDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public int? ShiftTemplateId { get; set; }
    public string? ShiftTemplateName { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDurationMinutes { get; set; }
    public decimal TotalHours { get; set; }
    public ShiftType ShiftType { get; set; }
    public string ShiftTypeName { get; set; } = string.Empty;
    public string Color { get; set; } = "#2196F3";
    public string? Notes { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckInTime { get; set; }
    public bool IsCheckedOut { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO per statistiche turni dipendente
/// </summary>
public class EmployeeShiftStatsDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TotalHoursThisWeek { get; set; }
    public decimal TotalHoursThisMonth { get; set; }
    public decimal TotalHoursLastMonth { get; set; }
    public int TotalShiftsThisWeek { get; set; }
    public int TotalShiftsThisMonth { get; set; }
    public int TotalShiftsLastMonth { get; set; }
    public decimal AverageHoursPerShift { get; set; }
    public decimal? MaxHoursPerWeek { get; set; }
    public decimal? MaxHoursPerMonth { get; set; }
    public decimal RemainingHoursThisWeek { get; set; }
    public decimal RemainingHoursThisMonth { get; set; }
    public bool IsOverLimit { get; set; }
}
