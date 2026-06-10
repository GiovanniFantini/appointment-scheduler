using System.ComponentModel.DataAnnotations;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>Richiesta di impostazione della nuova password con token di verifica.</summary>
public class ResetPasswordRequest
{
    /// <summary>Token ricevuto via email per autorizzare il reset.</summary>
    [Required(ErrorMessage = "Token mancante. Apri di nuovo il link ricevuto via email.")]
    [StringLength(512, MinimumLength = 16, ErrorMessage = "Token non valido. Richiedi un nuovo recupero password.")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Nuova password da impostare. Policy: ≥12 caratteri, con almeno una
    /// maiuscola, una minuscola e una cifra (allineata ad AuthService.ValidatePassword).
    /// </summary>
    [Required(ErrorMessage = "Inserisci la nuova password.")]
    [StringLength(256, MinimumLength = 12, ErrorMessage = "La password deve essere di almeno 12 caratteri.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "La password deve contenere almeno una lettera maiuscola, una minuscola e una cifra.")]
    public string NewPassword { get; set; } = string.Empty;
}
