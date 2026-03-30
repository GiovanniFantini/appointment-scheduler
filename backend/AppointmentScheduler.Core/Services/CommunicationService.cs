using AppointmentScheduler.Core.Interfaces;

namespace AppointmentScheduler.Core.Services;

/// <summary>
/// Servizio di comunicazione generico che orchestra l'invio di messaggi tramite IEmailService.
/// Progettato per essere esteso a canali aggiuntivi come SMS e push notification.
/// </summary>
public class CommunicationService : ICommunicationService
{
    private readonly IEmailService _emailService;

    /// <summary>Inizializza il servizio con il provider email configurato.</summary>
    /// <param name="emailService">Implementazione del servizio email.</param>
    public CommunicationService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    /// <summary>Invia un messaggio email tramite il provider configurato.</summary>
    /// <param name="toAddress">Indirizzo email del destinatario.</param>
    /// <param name="toDisplayName">Nome visualizzato del destinatario.</param>
    /// <param name="subject">Oggetto del messaggio.</param>
    /// <param name="htmlBody">Corpo del messaggio in formato HTML.</param>
    public Task SendEmailAsync(string toAddress, string toDisplayName, string subject, string htmlBody)
        => _emailService.SendAsync(toAddress, toDisplayName, subject, htmlBody);
}
