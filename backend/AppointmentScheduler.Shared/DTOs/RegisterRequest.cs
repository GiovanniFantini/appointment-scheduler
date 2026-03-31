namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Registrazione merchant: crea User (AccountType=Merchant) + Merchant.
/// </summary>
public class RegisterMerchantRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? VatNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? BusinessPhone { get; set; }
    public string? BusinessEmail { get; set; }
}
