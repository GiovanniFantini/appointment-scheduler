namespace AppointmentScheduler.Core.Interfaces;

/// <summary>Servizio per la gestione del flusso di recupero password tramite email.</summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Genera un token di reset e invia l'email all'utente.
    /// Risponde sempre true anche se l'email non esiste (protezione anti-enumeration).
    /// </summary>
    /// <param name="email">Indirizzo email dell'account.</param>
    /// <returns>True in ogni caso.</returns>
    Task<bool> RequestPasswordResetAsync(string email);

    /// <summary>
    /// Valida il token e imposta la nuova password.
    /// </summary>
    /// <param name="token">Token ricevuto via email.</param>
    /// <param name="newPassword">Nuova password (minimo 8 caratteri).</param>
    /// <returns>True se il reset e' avvenuto con successo, false se il token non e' valido, scaduto o gia' usato.</returns>
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}
