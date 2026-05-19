using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppointmentScheduler.Core.Interfaces;
using AppointmentScheduler.Shared.DTOs;

namespace AppointmentScheduler.API.Controllers;

/// <summary>Endpoint diagnostici riservati agli amministratori (test servizi infrastrutturali).</summary>
[ApiController]
[Route("api/admin/tools")]
[Authorize(Policy = "AdminOnly")]
public class AdminToolsController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminToolsController> _logger;

    public AdminToolsController(IEmailService emailService, ILogger<AdminToolsController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>Ritorna lo stato di configurazione del provider email (senza esporre segreti).</summary>
    [HttpGet("email/status")]
    public ActionResult<EmailServiceStatusDto> GetEmailStatus()
    {
        var status = _emailService.GetStatus();
        return Ok(new EmailServiceStatusDto
        {
            IsConfigured = status.IsConfigured,
            SenderAddress = status.SenderAddress,
            SenderDisplayName = status.SenderDisplayName,
            EndpointHost = status.EndpointHost,
        });
    }

    /// <summary>Invia un'email di test diagnostica all'indirizzo specificato.</summary>
    [HttpPost("email/test")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var status = _emailService.GetStatus();
        if (!status.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                isConfigured = false,
                message = "Azure Communication Services non configurato. Imposta AzureCommunicationServices:ConnectionString in appsettings o nelle App Settings."
            });
        }

        var displayName = string.IsNullOrWhiteSpace(request.ToDisplayName)
            ? request.ToAddress
            : request.ToDisplayName!;
        var sentAt = DateTimeOffset.UtcNow;
        var subject = "Email di test - Appointment Scheduler Admin";
        var html = BuildTestEmailHtml(request.ToAddress, sentAt);

        try
        {
            await _emailService.SendAsync(request.ToAddress, displayName, subject, html);
            _logger.LogInformation("Email di test inviata da admin tools a {Recipient}", request.ToAddress);
            return Ok(new
            {
                success = true,
                sentAt = sentAt.ToString("o"),
                recipient = request.ToAddress,
            });
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Errore ACS durante invio email di test a {Recipient}", request.ToAddress);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                success = false,
                status = ex.Status,
                errorCode = ex.ErrorCode,
                message = ex.Message,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore inatteso durante invio email di test a {Recipient}", request.ToAddress);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Errore interno durante l'invio dell'email di test.",
            });
        }
    }

    private static string BuildTestEmailHtml(string recipient, DateTimeOffset sentAt)
    {
        var when = sentAt.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        return $@"<!DOCTYPE html>
<html lang=""it"">
<head><meta charset=""UTF-8""></head>
<body style=""margin:0;padding:0;background:#0f172a;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#e2e8f0;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#0f172a;padding:32px 0;"">
    <tr><td align=""center"">
      <table role=""presentation"" width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background:#1e293b;border-radius:12px;overflow:hidden;"">
        <tr><td style=""background:linear-gradient(135deg,#0ea5e9,#6366f1);padding:24px;color:#fff;font-size:18px;font-weight:600;"">
          Appointment Scheduler - Email di Test
        </td></tr>
        <tr><td style=""padding:28px 28px 8px;"">
          <p style=""margin:0 0 12px;font-size:15px;line-height:1.6;"">
            Questa è un'email di test generata dall'Admin Hub per verificare il funzionamento del servizio email tramite Azure Communication Services.
          </p>
          <p style=""margin:0 0 12px;font-size:14px;color:#94a3b8;"">
            Se ricevi questo messaggio, il provider è configurato correttamente.
          </p>
        </td></tr>
        <tr><td style=""padding:8px 28px 28px;font-size:13px;color:#94a3b8;"">
          <div><strong>Destinatario:</strong> {System.Net.WebUtility.HtmlEncode(recipient)}</div>
          <div><strong>Inviata:</strong> {when}</div>
        </td></tr>
        <tr><td style=""padding:16px 28px;background:#0f172a;font-size:11px;color:#475569;text-align:center;"">
          Messaggio diagnostico - non rispondere
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
    }
}
