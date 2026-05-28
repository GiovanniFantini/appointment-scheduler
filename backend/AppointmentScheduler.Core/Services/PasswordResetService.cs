using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Enums;
using AppointmentScheduler.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AppointmentScheduler.Core.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly FrontendUrlOptions _frontendUrlOptions;
    private readonly IPasswordResetTokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUtcClock _clock;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IApplicationDbContext context,
        IEmailService emailService,
        FrontendUrlOptions frontendUrlOptions,
        IPasswordResetTokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IUtcClock clock,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _emailService = emailService;
        _frontendUrlOptions = frontendUrlOptions;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _clock = clock;
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

        // Rate limiting: max 1 richiesta ogni 60 secondi. Quando scatta restituiamo
        // comunque true (come per l'email non trovata): una risposta diversa
        // permetterebbe di enumerare gli account. Il countdown lato client è
        // persistito per impedire all'utente legittimo di reinviare a vuoto.
        var now = _clock.UtcNow;
        var recentToken = await _context.PasswordResetTokens
            .AnyAsync(t => t.UserId == user.Id && t.CreatedAt > now.AddSeconds(-60));

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
            pending.UsedAt = now;

        var tokenValue = _tokenGenerator.Generate();

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = tokenValue,
            CreatedAt = now,
            ExpiresAt = now.AddHours(1)
        });

        await _context.SaveChangesAsync();

        var frontendBaseUrl = GetFrontendBaseUrl(user);
        var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={WebUtility.UrlEncode(tokenValue)}";
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

        var normalizedToken = NormalizeIncomingToken(token);

        // Stessa lunghezza minima dei flussi di registrazione (vedi AuthService).
        if (newPassword.Length < AuthService.MinPasswordLength)
            return false;

        var now = _clock.UtcNow;

        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.Token == normalizedToken &&
                t.UsedAt == null &&
                t.ExpiresAt > now);

        if (resetToken == null)
            return false;

        resetToken.User.PasswordHash = _passwordHasher.HashPassword(newPassword);
        resetToken.User.UpdatedAt = now;
        resetToken.UsedAt = now;

        await _context.SaveChangesAsync();
        return true;
    }

    private string GetFrontendBaseUrl(User user)
    {
        return _frontendUrlOptions.GetBaseUrl(user.AccountType);
    }

    private static string NormalizeIncomingToken(string token)
    {
        var trimmed = token.Trim();

        // Supporta token passati in querystring legacy dove '+' puo diventare spazio.
        return WebUtility.UrlDecode(trimmed).Replace(' ', '+');
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
