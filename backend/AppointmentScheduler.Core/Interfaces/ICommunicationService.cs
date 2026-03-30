namespace AppointmentScheduler.Core.Interfaces;

/// <summary>
/// Servizio generico di comunicazione verso gli utenti.
/// Progettato per essere esteso a canali aggiuntivi come SMS e push notification.
/// </summary>
public interface ICommunicationService
{
    /// <summary>Invia un messaggio email tramite il provider configurato.</summary>
    /// <param name="toAddress">Indirizzo email del destinatario.</param>
    /// <param name="toDisplayName">Nome visualizzato del destinatario.</param>
    /// <param name="subject">Oggetto del messaggio.</param>
    /// <param name="htmlBody">Corpo del messaggio in formato HTML.</param>
    Task SendEmailAsync(string toAddress, string toDisplayName, string subject, string htmlBody);
}
