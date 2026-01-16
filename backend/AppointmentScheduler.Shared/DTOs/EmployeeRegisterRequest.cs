namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la registrazione di un employee nell'app
/// L'email deve corrispondere a un employee già censito dal merchant
/// </summary>
public class EmployeeRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
