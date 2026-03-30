namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione di un messaggio nella bacheca
/// </summary>
public class CreateBoardMessageRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public string Category { get; set; } = "Generale";
    public bool IsPinned { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// DTO per l'aggiornamento di un messaggio nella bacheca
/// </summary>
public class UpdateBoardMessageRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public string Category { get; set; } = "Generale";
    public bool IsPinned { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// DTO di risposta per un messaggio nella bacheca
/// </summary>
public class BoardMessageDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public int AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Numero totale di dipendenti che hanno letto il messaggio
    /// </summary>
    public int ReadCount { get; set; }

    /// <summary>
    /// Se il dipendente corrente ha letto il messaggio (per employee view)
    /// </summary>
    public bool IsReadByCurrentUser { get; set; }
}
