using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppointmentScheduler.Core.Interfaces;

namespace AppointmentScheduler.Core.Services;

/// <summary>Implementazione di IEmailService tramite Azure Communication Services Email SDK.</summary>
public class AzureEmailService : IEmailService
{
    private readonly ILogger<AzureEmailService> _logger;
    private readonly string _senderAddress;
    private readonly string _senderDisplayName;
    private readonly bool _isConfigured;
    private readonly EmailClient? _emailClient;

    /// <summary>Inizializza il servizio leggendo la configurazione da IConfiguration.</summary>
    /// <param name="configuration">Configurazione applicazione (sezione AzureCommunicationServices).</param>
    /// <param name="logger">Logger per diagnostica.</param>
    public AzureEmailService(IConfiguration configuration, ILogger<AzureEmailService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureCommunicationServices:ConnectionString"];
        _senderAddress = configuration["AzureCommunicationServices:SenderAddress"]
            ?? "DoNotReply@azurecomm.net";
        _senderDisplayName = configuration["AzureCommunicationServices:SenderDisplayName"]
            ?? "Appointment Scheduler";

        if (string.IsNullOrEmpty(connectionString) ||
            connectionString.Contains("CONFIGURE_IN_PRODUCTION_OR_USE_DEVELOPMENT_SETTINGS"))
        {
            _isConfigured = false;
            _logger.LogWarning("Azure Communication Services non configurato (access key mancante o placeholder). Le email non verranno inviate.");
        }
        else
        {
            _isConfigured = true;
            _emailClient = new EmailClient(connectionString);
        }
    }

    /// <summary>Invia un'email tramite Azure Communication Services. In assenza di configurazione, logga il contenuto.</summary>
    /// <param name="toAddress">Indirizzo email del destinatario.</param>
    /// <param name="toDisplayName">Nome visualizzato del destinatario.</param>
    /// <param name="subject">Oggetto dell'email.</param>
    /// <param name="htmlBody">Corpo dell'email in formato HTML.</param>
    public async Task SendAsync(string toAddress, string toDisplayName, string subject, string htmlBody)
    {
        if (!_isConfigured || _emailClient == null)
        {
            _logger.LogInformation(
                "MODALITA' SVILUPPO - Email non inviata. Destinatario: {To}, Oggetto: {Subject}",
                toAddress, subject);
            return;
        }

        var emailMessage = new EmailMessage(
            senderAddress: _senderAddress,
            content: new EmailContent(subject)
            {
                Html = htmlBody
            },
            recipients: new EmailRecipients(new List<EmailAddress>
            {
                new EmailAddress(toAddress, toDisplayName)
            })
        );

        try
        {
            var operation = await _emailClient.SendAsync(WaitUntil.Completed, emailMessage);
            _logger.LogInformation(
                "Email inviata con successo. OperationId: {OperationId}, Destinatario: {To}",
                operation.Id, toAddress);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex,
                "Errore ACS nell'invio email a {To}. Status: {Status}, ErrorCode: {ErrorCode}",
                toAddress, ex.Status, ex.ErrorCode);
            throw;
        }
    }
}
