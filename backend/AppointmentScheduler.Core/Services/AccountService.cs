using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.Core.Services;

public class AccountService : IAccountService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public AccountService(IApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<AccountProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return user == null ? null : ToDto(user);
    }

    public async Task<AccountProfileDto?> UpdateProfileAsync(int userId, UpdateAccountProfileRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        user.FirstName = (request.FirstName ?? string.Empty).Trim();
        user.LastName = (request.LastName ?? string.Empty).Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    public async Task<(bool Ok, string? ErrorMessage)> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.CurrentPassword))
            return (false, "Password attuale obbligatoria");
        if (!AuthService.TryValidatePassword(request.NewPassword, out var pwdError))
            return (false, pwdError);
        if (request.CurrentPassword == request.NewPassword)
            return (false, "La nuova password deve essere diversa da quella attuale");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return (false, "Utente non trovato");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return (false, "Password attuale non corretta");

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private static AccountProfileDto ToDto(Shared.Models.User user) => new()
    {
        UserId = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        AccountType = user.AccountType
    };
}
