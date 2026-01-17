using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Traccia anomalie rilevate nei turni con approccio empatico
/// </summary>
public class ShiftAnomaly
{
    public int Id { get; set; }
    public int ShiftId { get; set; }

    /// <summary>
    /// Tipo di anomalia rilevata
    /// </summary>
    public AnomalyType Type { get; set; }

    /// <summary>
    /// Severità dell'anomalia (1-5, dove 1 è minore)
    /// </summary>
    public int Severity { get; set; } = 1;

    /// <summary>
    /// Messaggio empatico generato dal sistema
    /// Es: "Noto che oggi sei arrivato più tardi del solito, va tutto bene?"
    /// </summary>
    public string EmpatheticMessage { get; set; } = string.Empty;

    /// <summary>
    /// Motivo fornito dal dipendente
    /// </summary>
    public AnomalyReason? EmployeeReason { get; set; }

    /// <summary>
    /// Note aggiuntive del dipendente
    /// </summary>
    public string? EmployeeNotes { get; set; }

    /// <summary>
    /// Se l'anomalia è stata risolta
    /// </summary>
    public bool IsResolved { get; set; } = false;

    /// <summary>
    /// Come è stata risolta (1-click, auto-corretta, merchant approval)
    /// </summary>
    public string? ResolutionMethod { get; set; }

    /// <summary>
    /// Note del merchant (se richiede revisione)
    /// </summary>
    public string? MerchantNotes { get; set; }

    /// <summary>
    /// Timestamp rilevamento
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp risoluzione
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Se richiede attenzione del merchant
    /// </summary>
    public bool RequiresMerchantReview { get; set; } = false;

    // Navigation properties
    public Shift Shift { get; set; } = null!;
}
