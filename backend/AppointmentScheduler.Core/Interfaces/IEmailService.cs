namespace AppointmentScheduler.Core.Interfaces;

/// <summary>Servizio per l'invio di email tramite un provider esterno.</summary>
public interface IEmailService
{
    /// <summary>Invia un'email con corpo HTML.</summary>
    /// <param name="toAddress">Indirizzo email del destinatario.</param>
    /// <param name="toDisplayName">Nome visualizzato del destinatario.</param>
    /// <param name="subject">Oggetto dell'email.</param>
    /// <param name="htmlBody">Corpo dell'email in formato HTML.</param>
    Task SendAsync(string toAddress, string toDisplayName, string subject, string htmlBody);
}
