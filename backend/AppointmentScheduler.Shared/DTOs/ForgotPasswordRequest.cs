namespace AppointmentScheduler.Shared.DTOs;

/// <summary>Richiesta di reset password tramite email.</summary>
public class ForgotPasswordRequest
{
    /// <summary>Indirizzo email dell'account per cui si richiede il reset.</summary>
    public string Email { get; set; } = string.Empty;
}
