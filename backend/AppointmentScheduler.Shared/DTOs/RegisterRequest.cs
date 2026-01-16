namespace AppointmentScheduler.Shared.DTOs;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    // Registrazione base come User (consumer)
    // I ruoli merchant/employee vengono attivati in seguito

    // Se vuole registrarsi anche come Merchant
    public bool RegisterAsMerchant { get; set; } = false;
    public string? BusinessName { get; set; }
    public string? VatNumber { get; set; }
}
