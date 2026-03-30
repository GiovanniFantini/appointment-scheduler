namespace AppointmentScheduler.Shared.DTOs;

/// <summary>Richiesta di impostazione della nuova password con token di verifica.</summary>
public class ResetPasswordRequest
{
    /// <summary>Token ricevuto via email per autorizzare il reset.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Nuova password da impostare (minimo 8 caratteri).</summary>
    public string NewPassword { get; set; } = string.Empty;
}
