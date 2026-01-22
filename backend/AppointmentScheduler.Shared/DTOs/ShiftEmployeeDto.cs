namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per dipendente assegnato a un turno
/// </summary>
public class ShiftEmployeeDto
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckInTime { get; set; }
    public bool IsCheckedOut { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? CheckInLocation { get; set; }
    public string? CheckOutLocation { get; set; }
    public string? Notes { get; set; }
}
