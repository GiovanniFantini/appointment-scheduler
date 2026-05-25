using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Profilo dell'utente loggato. Restituito da GET /api/account/me.
/// </summary>
public class AccountProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public AccountType AccountType { get; set; }
}

/// <summary>
/// Aggiornamento dei dati anagrafici dell'utente loggato.
/// L'email NON è modificabile (richiederebbe verifica) ed è omessa di proposito.
/// </summary>
public class UpdateAccountProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Cambio password self-service: richiede password corrente per evitare account hijack
/// in caso di sessione lasciata aperta.
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
