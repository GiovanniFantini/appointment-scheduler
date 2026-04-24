using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class EmployeeRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string EmployeeInitials { get; set; } = string.Empty;
    public int MerchantId { get; set; }
    public EmployeeRequestType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>Ora di inizio per permessi orari. Null = tutto il giorno.</summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>Ora di fine per permessi orari. Null = tutto il giorno.</summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>Id del turno a cui è collegata la richiesta. Null = applicabile a tutti i turni del giorno.</summary>
    public int? EventId { get; set; }

    public string? Notes { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEmployeeRequestRequest
{
    public EmployeeRequestType Type { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? EventId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateEmployeeRequestRequest
{
    public EmployeeRequestType Type { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? EventId { get; set; }
    public string? Notes { get; set; }
}

public class ReviewEmployeeRequestRequest
{
    public string? ReviewNotes { get; set; }
    public int? EventId { get; set; }
}
