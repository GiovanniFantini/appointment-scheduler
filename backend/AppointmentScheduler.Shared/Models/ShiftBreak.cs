namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Rappresenta una pausa effettiva presa durante un turno
/// Supporta pause multiple e tracking accurato
/// </summary>
public class ShiftBreak
{
    public int Id { get; set; }
    public int ShiftId { get; set; }

    /// <summary>
    /// Inizio pausa
    /// </summary>
    public DateTime BreakStartTime { get; set; }

    /// <summary>
    /// Fine pausa (nullable se pausa in corso)
    /// </summary>
    public DateTime? BreakEndTime { get; set; }

    /// <summary>
    /// Durata in minuti (calcolata automaticamente)
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Tipo di pausa (Pranzo, Caffè, Personale, etc.)
    /// </summary>
    public string BreakType { get; set; } = "General";

    /// <summary>
    /// Se la pausa è stata auto-suggerita dal sistema
    /// </summary>
    public bool IsAutoSuggested { get; set; } = false;

    /// <summary>
    /// Note opzionali
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Se è una pausa breve (<15min) opzionale
    /// </summary>
    public bool IsShortBreak { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Shift Shift { get; set; } = null!;
}
