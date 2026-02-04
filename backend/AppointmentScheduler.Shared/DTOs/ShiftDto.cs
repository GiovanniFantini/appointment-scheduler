using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione di un turno
/// </summary>
public class CreateShiftRequest
{
    public int? ShiftTemplateId { get; set; }

    /// <summary>
    /// DEPRECATED: Usare EmployeeIds per assegnazioni multiple
    /// </summary>
    [Obsolete("Use EmployeeIds for multi-employee assignment")]
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Lista di ID dipendenti da assegnare al turno
    /// </summary>
    public List<int>? EmployeeIds { get; set; }

    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDurationMinutes { get; set; } = 0;
    public ShiftType ShiftType { get; set; }
    public string Color { get; set; } = "#2196F3";
    public string? Notes { get; set; }

    /// <summary>
    /// Se true, forza la creazione del turno anche in caso di conflitto con ferie/permessi
    /// </summary>
    public bool ForceCreate { get; set; } = false;
}

/// <summary>
/// DTO per la creazione multipla di turni da template
/// </summary>
public class CreateShiftsFromTemplateRequest
{
    public int ShiftTemplateId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// DEPRECATED: Usare EmployeeIds per assegnazioni multiple
    /// </summary>
    [Obsolete("Use EmployeeIds for multi-employee assignment")]
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Lista di ID dipendenti da assegnare ai turni
    /// </summary>
    public List<int>? EmployeeIds { get; set; }

    public List<DayOfWeek>? DaysOfWeek { get; set; }

    /// <summary>
    /// Se true, forza la creazione dei turni anche in caso di conflitto con ferie/permessi
    /// </summary>
    public bool ForceCreate { get; set; } = false;
}

/// <summary>
/// DTO per l'aggiornamento di un turno
/// </summary>
public class UpdateShiftRequest
{
    /// <summary>
    /// DEPRECATED: Usare EmployeeIds per assegnazioni multiple
    /// </summary>
    [Obsolete("Use EmployeeIds for multi-employee assignment")]
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Lista di ID dipendenti da assegnare al turno
    /// </summary>
    public List<int>? EmployeeIds { get; set; }

    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDurationMinutes { get; set; } = 0;
    public ShiftType ShiftType { get; set; }
    public string Color { get; set; } = "#2196F3";
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Se true, forza l'aggiornamento del turno anche in caso di conflitto con ferie/permessi
    /// </summary>
    public bool ForceCreate { get; set; } = false;
}

/// <summary>
/// DTO per assegnare un turno a uno o più dipendenti
/// </summary>
public class AssignShiftRequest
{
    /// <summary>
    /// DEPRECATED: Usare EmployeeIds per assegnazioni multiple
    /// </summary>
    [Obsolete("Use EmployeeIds for multi-employee assignment")]
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Lista di ID dipendenti da assegnare al turno
    /// </summary>
    public List<int>? EmployeeIds { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Se true, forza l'assegnazione del turno anche in caso di conflitto con ferie/permessi
    /// </summary>
    public bool ForceCreate { get; set; } = false;
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

    /// <summary>
    /// DEPRECATED: Usare Employees per assegnazioni multiple
    /// Mantenuto per backward compatibility
    /// </summary>
    [Obsolete("Use Employees list for multi-employee assignment")]
    public int? EmployeeId { get; set; }

    /// <summary>
    /// DEPRECATED: Usare Employees per assegnazioni multiple
    /// Mantenuto per backward compatibility
    /// </summary>
    [Obsolete("Use Employees list for multi-employee assignment")]
    public string? EmployeeName { get; set; }

    /// <summary>
    /// Lista dipendenti assegnati al turno
    /// </summary>
    public List<ShiftEmployeeDto> Employees { get; set; } = new List<ShiftEmployeeDto>();

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
    public ValidationStatus ValidationStatus { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public string? ValidatedBy { get; set; }
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

/// <summary>
/// Informazioni su un conflitto tra turno e richiesta di ferie/permessi
/// </summary>
public class LeaveConflictInfo
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int LeaveRequestId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Risposta in caso di conflitto con ferie/permessi
/// </summary>
public class LeaveConflictResponse
{
    public string Message { get; set; } = string.Empty;
    public List<LeaveConflictInfo> Conflicts { get; set; } = new();
}
