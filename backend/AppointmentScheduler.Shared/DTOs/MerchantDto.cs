using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la risposta con i dati del merchant
/// </summary>
public class MerchantDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? VatNumber { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public UserDto User { get; set; } = null!;
}

/// <summary>
/// DTO per i dati utente nel contesto merchant
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
