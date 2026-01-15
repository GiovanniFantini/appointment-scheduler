using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> RegisterEmployeeAsync(EmployeeRegisterRequest request);
    string GenerateJwtToken(int userId, string email, string role, int? merchantId = null, int? employeeId = null);
}
