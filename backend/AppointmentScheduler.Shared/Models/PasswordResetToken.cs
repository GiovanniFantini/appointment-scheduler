namespace AppointmentScheduler.Shared.Models;

/// <summary>Token monouso per il reset della password, con scadenza temporale.</summary>
public class PasswordResetToken
{
    /// <summary>Identificatore univoco del token.</summary>
    public int Id { get; set; }

    /// <summary>Riferimento all'utente proprietario del token.</summary>
    public int UserId { get; set; }

    /// <summary>Valore del token, base64url-encoded (32 byte casuali). Univoco e indicizzato.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Data e ora di creazione del token (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Data e ora di scadenza del token (UTC). Di default 1 ora dopo la creazione.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Data e ora di utilizzo del token. Null se non ancora usato o invalidato.</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>Utente associato al token.</summary>
    public User User { get; set; } = null!;
}
