using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.Core.Interfaces;

public interface IAccountService
{
    Task<AccountProfileDto?> GetProfileAsync(int userId);
    Task<AccountProfileDto?> UpdateProfileAsync(int userId, UpdateAccountProfileRequest request);

    /// <summary>
    /// Risultato del cambio password: Ok=true se andato a buon fine, altrimenti
    /// ErrorMessage spiega cosa è andato storto (password corrente errata, nuova troppo debole, ecc.).
    /// </summary>
    Task<(bool Ok, string? ErrorMessage)> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}
