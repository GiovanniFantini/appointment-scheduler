using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione di un template turno
/// </summary>
public class CreateShiftTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ShiftType ShiftType { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDurationMinutes { get; set; } = 0;
    public RecurrencePattern RecurrencePattern { get; set; } = RecurrencePattern.None;
    public string? RecurrenceDays { get; set; }
    public string Color { get; set; } = "#2196F3";
}

/// <summary>
/// DTO per l'aggiornamento di un template turno
/// </summary>
public class UpdateShiftTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ShiftType ShiftType { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDurationMinutes { get; set; } = 0;
    public RecurrencePattern RecurrencePattern { get; set; } = RecurrencePattern.None;
    public string? RecurrenceDays { get; set; }
    public string Color { get; set; } = "#2196F3";
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO per la risposta con i dati del template turno
/// </summary>
public class ShiftTemplateDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ShiftType ShiftType { get; set; }
    public string ShiftTypeName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDurationMinutes { get; set; }
    public decimal TotalHours { get; set; } // Ore totali calcolate
    public RecurrencePattern RecurrencePattern { get; set; }
    public string? RecurrenceDays { get; set; }
    public string Color { get; set; } = "#2196F3";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
