using System.ComponentModel.DataAnnotations;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Registrazione merchant: crea User (AccountType=Merchant) + Merchant.
/// </summary>
public class RegisterMerchantRequest
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    // La policy di complessità (≥12 + maiuscola/minuscola/cifra) è applicata
    // dal servizio in AuthService.ValidatePassword: qui limitiamo solo lunghezza
    // minima e massima per fermare input chiaramente invalidi a ModelState.
    [Required]
    [StringLength(256, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(32)]
    public string? VatNumber { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(32)]
    public string? BusinessPhone { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? BusinessEmail { get; set; }
}
