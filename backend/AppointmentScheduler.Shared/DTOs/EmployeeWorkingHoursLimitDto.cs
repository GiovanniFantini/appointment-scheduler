namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione di limiti orari dipendente
/// </summary>
public class CreateEmployeeWorkingHoursLimitRequest
{
    public int EmployeeId { get; set; }
    public decimal? MaxHoursPerDay { get; set; }
    public decimal? MaxHoursPerWeek { get; set; }
    public decimal? MaxHoursPerMonth { get; set; }
    public decimal? MinHoursPerWeek { get; set; }
    public decimal? MinHoursPerMonth { get; set; }
    public bool AllowOvertime { get; set; } = false;
    public decimal? MaxOvertimeHoursPerWeek { get; set; }
    public decimal? MaxOvertimeHoursPerMonth { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }
}

/// <summary>
/// DTO per l'aggiornamento di limiti orari dipendente
/// </summary>
public class UpdateEmployeeWorkingHoursLimitRequest
{
    public decimal? MaxHoursPerDay { get; set; }
    public decimal? MaxHoursPerWeek { get; set; }
    public decimal? MaxHoursPerMonth { get; set; }
    public decimal? MinHoursPerWeek { get; set; }
    public decimal? MinHoursPerMonth { get; set; }
    public bool AllowOvertime { get; set; } = false;
    public decimal? MaxOvertimeHoursPerWeek { get; set; }
    public decimal? MaxOvertimeHoursPerMonth { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO per la risposta con i dati dei limiti orari dipendente
/// </summary>
public class EmployeeWorkingHoursLimitDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int MerchantId { get; set; }
    public decimal? MaxHoursPerDay { get; set; }
    public decimal? MaxHoursPerWeek { get; set; }
    public decimal? MaxHoursPerMonth { get; set; }
    public decimal? MinHoursPerWeek { get; set; }
    public decimal? MinHoursPerMonth { get; set; }
    public bool AllowOvertime { get; set; }
    public decimal? MaxOvertimeHoursPerWeek { get; set; }
    public decimal? MaxOvertimeHoursPerMonth { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
