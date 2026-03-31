using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginMerchantAsync(LoginRequest request);
    Task<AuthResponse?> RegisterMerchantAsync(RegisterMerchantRequest request);
    Task<AuthResponse?> LoginEmployeeAsync(LoginRequest request);
    Task<AuthResponse?> RegisterEmployeeAsync(EmployeeRegisterRequest request);
    Task<AuthResponse?> SelectCompanyAsync(int userId, int merchantId);
    Task<AuthResponse?> LoginAdminAsync(LoginRequest request);
    string GenerateJwtToken(int userId, string email, string role, int? merchantId = null, int? employeeId = null, List<string>? features = null);
}
