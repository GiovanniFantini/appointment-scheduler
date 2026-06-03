using System.ComponentModel.DataAnnotations;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>Richiesta di impostazione della nuova password con token di verifica.</summary>
public class ResetPasswordRequest
{
    /// <summary>Token ricevuto via email per autorizzare il reset.</summary>
    [Required]
    [StringLength(512, MinimumLength = 16)]
    public string Token { get; set; } = string.Empty;

    /// <summary>Nuova password da impostare (≥12 caratteri, con complessità minima validata lato service).</summary>
    [Required]
    [StringLength(256, MinimumLength = 12)]
    public string NewPassword { get; set; } = string.Empty;
}
