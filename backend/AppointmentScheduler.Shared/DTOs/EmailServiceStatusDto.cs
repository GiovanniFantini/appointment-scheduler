namespace AppointmentScheduler.Shared.DTOs;

/// <summary>Stato del servizio email esposto al client (senza segreti).</summary>
public class EmailServiceStatusDto
{
    /// <summary>Indica se il provider email è configurato e pronto a inviare.</summary>
    public bool IsConfigured { get; set; }

    /// <summary>Indirizzo email mittente configurato.</summary>
    public string SenderAddress { get; set; } = string.Empty;

    /// <summary>Display name del mittente configurato.</summary>
    public string SenderDisplayName { get; set; } = string.Empty;

    /// <summary>Host del provider email (es. dominio Azure Communication Services). Null se non determinabile.</summary>
    public string? EndpointHost { get; set; }
}
