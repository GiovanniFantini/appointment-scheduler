namespace AppointmentScheduler.Core.Interfaces;

/// <summary>Esito della validazione/uso di un token di reset password.</summary>
public enum ResetPasswordResult
{
    /// <summary>Password aggiornata con successo.</summary>
    Success,

    /// <summary>Token mancante o nuova password non valida (es. troppo corta).</summary>
    InvalidInput,

    /// <summary>Token inesistente o non corrispondente a nessun record.</summary>
    TokenNotFound,

    /// <summary>Token trovato ma scaduto (ExpiresAt nel passato).</summary>
    TokenExpired,

    /// <summary>Token trovato ma gia' utilizzato o invalidato (UsedAt valorizzato).</summary>
    TokenAlreadyUsed
}

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
    /// <returns>L'esito tipizzato del reset: successo, oppure il motivo specifico del fallimento.</returns>
    Task<ResetPasswordResult> ResetPasswordAsync(string token, string newPassword);
}
