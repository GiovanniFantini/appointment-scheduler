namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Rappresenta un messaggio/comunicazione nella bacheca aziendale
/// </summary>
public class BoardMessage
{
    public int Id { get; set; }
    public int MerchantId { get; set; }

    /// <summary>
    /// UserId dell'autore del messaggio
    /// </summary>
    public int AuthorUserId { get; set; }

    /// <summary>
    /// Titolo del messaggio
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Contenuto del messaggio
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Priorità: 0 = Normale, 1 = Importante, 2 = Urgente
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Categoria del messaggio (es: "Generale", "Turni", "HR", "Evento")
    /// </summary>
    public string Category { get; set; } = "Generale";

    /// <summary>
    /// Se il messaggio è fissato in alto nella bacheca
    /// </summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// Data di scadenza del messaggio (dopo la quale non viene più mostrato)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public User Author { get; set; } = null!;
    public ICollection<BoardMessageRead> ReadReceipts { get; set; } = new List<BoardMessageRead>();
}
