namespace AppointmentScheduler.API.Authorization;

/// <summary>
/// Messaggi generici usati quando una richiesta di autenticazione viene rifiutata
/// per superamento di una soglia: rate limit per IP, rate limit per email o
/// lockout dell'account dopo troppi tentativi falliti.
///
/// La copy è deliberatamente uniforme tra i tre scenari: un client automatizzato
/// non deve poter distinguere "IP throttled" da "account locked" da
/// "policy auth-email triggerata" leggendo il body della risposta. Cambia solo
/// l'ammontare di tempo da attendere.
///
/// Vincoli sul testo:
/// - Niente termini tecnici ("rate limit", "blocco", "tentativi", "lockout").
/// - Niente riferimenti all'account o all'endpoint colpito.
/// - Tempo arrotondato verso l'alto al minuto, con fallback vago quando la
///   stima non è disponibile.
/// </summary>
public static class AuthThrottleMessages
{
    /// <summary>
    /// Restituisce il messaggio da inviare al client quando una soglia di
    /// throttling è stata superata.
    /// </summary>
    /// <param name="minutes">
    /// Minuti residui prima del prossimo tentativo, già arrotondati per eccesso.
    /// Se ≤ 0 viene usato il fallback vago.
    /// </param>
    public static string GenericRetry(int minutes)
    {
        if (minutes <= 0)
            return "Operazione non disponibile al momento. Riprova tra qualche minuto.";

        var unit = minutes == 1 ? "minuto" : "minuti";
        return $"Operazione non disponibile al momento. Riprova tra circa {minutes} {unit}.";
    }

    /// <summary>
    /// Converte una durata residua in minuti arrotondati per eccesso. Usata sia
    /// dal middleware OnRejected del RateLimiter (a partire dalla metadata
    /// RetryAfter del lease) sia da AuthService per il lockout (LockedUntilUtc - now).
    /// </summary>
    public static int CeilToMinutes(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return 0;
        return (int)Math.Ceiling(duration.TotalMinutes);
    }
}
