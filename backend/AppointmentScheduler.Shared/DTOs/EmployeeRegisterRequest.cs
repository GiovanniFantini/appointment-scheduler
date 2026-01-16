namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la registrazione autonoma di un employee
/// L'employee si registra direttamente, poi i merchant possono collegarlo
/// </summary>
public class EmployeeRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
