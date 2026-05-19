namespace AppointmentScheduler.Core.Interfaces;

/// <summary>Stato di configurazione del servizio email.</summary>
public record EmailServiceStatus(
    bool IsConfigured,
    string SenderAddress,
    string SenderDisplayName,
    string? EndpointHost);

/// <summary>Servizio per l'invio di email tramite un provider esterno.</summary>
public interface IEmailService
{
    /// <summary>Invia un'email con corpo HTML.</summary>
    /// <param name="toAddress">Indirizzo email del destinatario.</param>
    /// <param name="toDisplayName">Nome visualizzato del destinatario.</param>
    /// <param name="subject">Oggetto dell'email.</param>
    /// <param name="htmlBody">Corpo dell'email in formato HTML.</param>
    Task SendAsync(string toAddress, string toDisplayName, string subject, string htmlBody);

    /// <summary>Ritorna lo stato corrente di configurazione del servizio email (senza esporre segreti).</summary>
    EmailServiceStatus GetStatus();
}
