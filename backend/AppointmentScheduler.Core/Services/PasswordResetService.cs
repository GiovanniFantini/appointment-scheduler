using System.Security.Cryptography;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Data;
using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Core.Services;

/// <summary>Gestisce il flusso di recupero password: generazione token, invio email, validazione e reset.</summary>
public class PasswordResetService : IPasswordResetService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;

    /// <summary>Inizializza il servizio con le dipendenze necessarie.</summary>
    /// <param name="context">DbContext per accesso ai dati.</param>
    /// <param name="emailService">Servizio email per invio notifiche.</param>
    /// <param name="configuration">Configurazione applicazione.</param>
    /// <param name="logger">Logger per diagnostica.</param>
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

    /// <summary>
    /// Genera un token di reset e invia l'email all'utente.
    /// Risponde sempre true (protezione anti-enumeration).
    /// Rifiuta la richiesta se un token e' stato generato negli ultimi 60 secondi.
    /// </summary>
    /// <param name="email">Indirizzo email dell'account.</param>
    /// <returns>True in ogni caso.</returns>
    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.IsActive);

        if (user == null)
        {
            _logger.LogInformation("Reset password richiesto per email non trovata: {Email}", normalizedEmail);
            return true;
        }

        // Rate limiting: rifiuta se esiste un token creato negli ultimi 60 secondi
        var recentToken = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.CreatedAt > DateTime.UtcNow.AddSeconds(-60))
            .AnyAsync();

        if (recentToken)
        {
            _logger.LogWarning("Rate limit reset password per UserId: {UserId}", user.Id);
            return true;
        }

        // Invalida tutti i token pendenti precedenti
        var pendingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync();

        foreach (var pending in pendingTokens)
            pending.UsedAt = DateTime.UtcNow;

        // Genera nuovo token base64url
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var tokenValue = Convert.ToBase64String(rawBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = tokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        var frontendBaseUrl = GetFrontendBaseUrl(user);

        var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?token={tokenValue}";
        var htmlBody = BuildResetEmailHtml(user.FirstName, resetUrl);

        try
        {
            await _emailService.SendAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                "Recupero password - Appointment Scheduler",
                htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore invio email reset per UserId: {UserId}", user.Id);
        }

        return true;
    }

    /// <summary>
    /// Valida il token e imposta la nuova password.
    /// </summary>
    /// <param name="token">Token ricevuto via email.</param>
    /// <param name="newPassword">Nuova password (minimo 8 caratteri).</param>
    /// <returns>True se il reset e' avvenuto con successo, false altrimenti.</returns>
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
        {
            _logger.LogWarning("Tentativo reset con token non valido o scaduto.");
            return false;
        }

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        resetToken.User.UpdatedAt = DateTime.UtcNow;
        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password resettata con successo per UserId: {UserId}", resetToken.UserId);
        return true;
    }

    /// <summary>Costruisce il corpo HTML dell'email di reset password.</summary>
    /// <param name="firstName">Nome dell'utente per personalizzare il messaggio.</param>
    /// <param name="resetUrl">URL completo con token per il reset.</param>
    /// <returns>Stringa HTML self-contained.</returns>
    private static string BuildResetEmailHtml(string firstName, string resetUrl)
    {
        return $$"""
            <!DOCTYPE html>
            <html lang="it">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>Recupero password</title>
              <style>
                body { margin: 0; padding: 0; background-color: #0f172a; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; }
                .container { max-width: 560px; margin: 40px auto; background: #1e293b; border-radius: 12px; border: 1px solid #334155; overflow: hidden; }
                .header { background: linear-gradient(135deg, #0ea5e9, #6366f1); padding: 32px 40px; text-align: center; }
                .header h1 { color: #ffffff; margin: 0; font-size: 22px; font-weight: 700; letter-spacing: 0.5px; }
                .body { padding: 40px; color: #cbd5e1; line-height: 1.6; }
                .body p { margin: 0 0 16px; font-size: 15px; }
                .body .name { color: #f1f5f9; font-weight: 600; }
                .button-wrap { text-align: center; margin: 32px 0; }
                .button { display: inline-block; background: linear-gradient(135deg, #0ea5e9, #6366f1); color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-size: 15px; font-weight: 600; letter-spacing: 0.3px; }
                .fallback { background: #0f172a; border-radius: 8px; padding: 16px; margin-top: 24px; font-size: 13px; color: #64748b; word-break: break-all; }
                .fallback a { color: #0ea5e9; text-decoration: none; }
                .footer { border-top: 1px solid #334155; padding: 24px 40px; text-align: center; font-size: 12px; color: #475569; }
                .warning { background: #1a1a2e; border-left: 3px solid #f59e0b; padding: 12px 16px; border-radius: 4px; margin-top: 16px; font-size: 13px; color: #94a3b8; }
              </style>
            </head>
            <body>
              <div class="container">
                <div class="header">
                  <h1>Appointment Scheduler</h1>
                </div>
                <div class="body">
                  <p>Ciao <span class="name">{{firstName}}</span>,</p>
                  <p>Abbiamo ricevuto una richiesta di recupero password per il tuo account.</p>
                  <p>Clicca sul pulsante seguente per impostare una nuova password:</p>
                  <div class="button-wrap">
                    <a href="{{resetUrl}}" class="button">Reimposta password</a>
                  </div>
                  <div class="warning">
                    Il link e' valido per <strong>1 ora</strong> e puo' essere utilizzato una sola volta.
                  </div>
                  <div class="fallback">
                    <p>Se il pulsante non funziona, copia e incolla questo indirizzo nel browser:</p>
                    <a href="{{resetUrl}}">{{resetUrl}}</a>
                  </div>
                  <p style="margin-top:24px;">Se non hai richiesto il recupero password, ignora questa email. Il tuo account rimane al sicuro.</p>
                </div>
                <div class="footer">
                  Questo messaggio e' stato inviato automaticamente. Non rispondere a questa email.
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private string GetFrontendBaseUrl(User user)
    {
        var section = "AzureCommunicationServices:FrontendBaseUrls:";

        if (user.IsAdmin)
            return _configuration[section + "Admin"] ?? "http://localhost:5175";
        if (user.IsMerchant)
            return _configuration[section + "Merchant"] ?? "http://localhost:5174";
        if (user.IsEmployee)
            return _configuration[section + "Employee"] ?? "http://localhost:5176";

        return _configuration[section + "Consumer"] ?? "http://localhost:5173";
    }
}
