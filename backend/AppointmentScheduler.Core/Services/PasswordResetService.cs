using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        ApplicationDbContext context,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive);

        if (user == null)
        {
            _logger.LogInformation("Reset password richiesto per email non trovata: {Email}", normalizedEmail);
            return true;
        }

        // Rate limiting: max 1 richiesta ogni 60 secondi
        var recentToken = await _context.PasswordResetTokens
            .AnyAsync(t => t.UserId == user.Id && t.CreatedAt > DateTime.UtcNow.AddSeconds(-60));

        if (recentToken)
        {
            _logger.LogWarning("Rate limit reset password per UserId: {UserId}", user.Id);
            return true;
        }

        // Invalida token precedenti
        var pendingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync();

        foreach (var pending in pendingTokens)
            pending.UsedAt = DateTime.UtcNow;

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var tokenValue = Convert.ToBase64String(rawBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = tokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });

        await _context.SaveChangesAsync();

        var frontendBaseUrl = GetFrontendBaseUrl(user);
        var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={tokenValue}";
        var htmlBody = BuildResetEmailHtml(user.FirstName, resetUrl);

        try
        {
            await _emailService.SendAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                "Recupero password",
                htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore invio email reset per UserId: {UserId}", user.Id);
        }

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            return false;

        if (newPassword.Length < 8)
            return false;

        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == token &&
                t.UsedAt == null &&
                t.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
            return false;

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        resetToken.User.UpdatedAt = DateTime.UtcNow;
        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private string GetFrontendBaseUrl(User user)
    {
        var section = "AzureCommunicationServices:FrontendBaseUrls:";
        return user.AccountType switch
        {
            AccountType.Admin => _configuration[section + "Admin"] ?? "http://localhost:5175",
            AccountType.Merchant => _configuration[section + "Merchant"] ?? "http://localhost:5174",
            AccountType.Employee => _configuration[section + "Employee"] ?? "http://localhost:5176",
            _ => "http://localhost:5173"
        };
    }

    private static string BuildResetEmailHtml(string firstName, string resetUrl) => $$"""
        <!DOCTYPE html>
        <html lang="it">
        <head>
          <meta charset="UTF-8">
          <title>Recupero password</title>
          <style>
            body { margin:0; padding:0; background:#0f172a; font-family:-apple-system,sans-serif; }
            .container { max-width:560px; margin:40px auto; background:#1e293b; border-radius:12px; border:1px solid #334155; }
            .header { background:linear-gradient(135deg,#0ea5e9,#6366f1); padding:32px 40px; text-align:center; }
            .header h1 { color:#fff; margin:0; font-size:22px; }
            .body { padding:40px; color:#cbd5e1; line-height:1.6; }
            .button-wrap { text-align:center; margin:32px 0; }
            .button { display:inline-block; background:linear-gradient(135deg,#0ea5e9,#6366f1); color:#fff; text-decoration:none; padding:14px 32px; border-radius:8px; font-weight:600; }
            .footer { border-top:1px solid #334155; padding:24px 40px; text-align:center; font-size:12px; color:#475569; }
          </style>
        </head>
        <body>
          <div class="container">
            <div class="header"><h1>Gestionale Aziendale</h1></div>
            <div class="body">
              <p>Ciao <strong>{{firstName}}</strong>,</p>
              <p>Abbiamo ricevuto una richiesta di recupero password per il tuo account.</p>
              <div class="button-wrap">
                <a href="{{resetUrl}}" class="button">Reimposta password</a>
              </div>
              <p>Il link è valido per <strong>1 ora</strong> e può essere utilizzato una sola volta.</p>
              <p>Se non hai richiesto il recupero password, ignora questa email.</p>
            </div>
            <div class="footer">Messaggio inviato automaticamente. Non rispondere.</div>
          </div>
        </body>
        </html>
        """;
}
