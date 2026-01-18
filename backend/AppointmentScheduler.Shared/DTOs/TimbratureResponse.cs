using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Response per check-in/check-out con messaggi contestuali
/// </summary>
public class TimbratureResponse
{
    /// <summary>
    /// Se l'operazione è riuscita
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Messaggio di conferma immediata (es: "✓ Entrata 9:03")
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Contesto (es: "Giornata 8h, pausa suggerita 13:00")
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Prossimi passi (es: "Ricorda uscita stasera")
    /// </summary>
    public string? NextSteps { get; set; }

    /// <summary>
    /// Messaggio empatico se necessario
    /// </summary>
    public string? EmpatheticMessage { get; set; }

    /// <summary>
    /// Se è stata rilevata un'anomalia
    /// </summary>
    public bool HasAnomaly { get; set; }

    /// <summary>
    /// Tipo di anomalia rilevata
    /// </summary>
    public AnomalyType? AnomalyType { get; set; }

    /// <summary>
    /// ID dell'anomalia creata
    /// </summary>
    public int? AnomalyId { get; set; }

    /// <summary>
    /// Opzioni rapide per risolvere l'anomalia
    /// </summary>
    public List<string>? QuickResolutionOptions { get; set; }

    /// <summary>
    /// Orario suggerito pausa
    /// </summary>
    public string? SuggestedBreakTime { get; set; }

    /// <summary>
    /// Durata totale turno (in ore)
    /// </summary>
    public decimal? TotalShiftHours { get; set; }

    /// <summary>
    /// Se ci sono straordinari
    /// </summary>
    public bool HasOvertime { get; set; }

    /// <summary>
    /// Durata straordinario in minuti
    /// </summary>
    public int? OvertimeMinutes { get; set; }

    /// <summary>
    /// Timestamp dell'operazione
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
