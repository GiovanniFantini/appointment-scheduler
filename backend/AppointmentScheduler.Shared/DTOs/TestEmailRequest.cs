using System.ComponentModel.DataAnnotations;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>Richiesta di invio di una email di test diagnostica (admin tools).</summary>
public class TestEmailRequest
{
    /// <summary>Indirizzo email del destinatario.</summary>
    [Required]
    [EmailAddress]
    public string ToAddress { get; set; } = string.Empty;

    /// <summary>Nome visualizzato del destinatario (opzionale).</summary>
    public string? ToDisplayName { get; set; }
}
