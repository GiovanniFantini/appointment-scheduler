using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Template riutilizzabile per turni ricorrenti
/// Permette al merchant di creare pattern di turni da applicare rapidamente
/// </summary>
public class ShiftTemplate
{
    public int Id { get; set; }
    public int MerchantId { get; set; }

    /// <summary>
    /// Nome identificativo del template (es: "Turno Mattina Weekday", "Weekend Evening")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione opzionale
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tipo di turno
    /// </summary>
    public ShiftType ShiftType { get; set; }

    /// <summary>
    /// Ora di inizio (formato TimeSpan per supportare turni oltre mezzanotte)
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Ora di fine
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Durata pausa in minuti (es: 30 minuti)
    /// </summary>
    public int BreakDurationMinutes { get; set; } = 0;

    /// <summary>
    /// Pattern di ricorrenza
    /// </summary>
    public RecurrencePattern RecurrencePattern { get; set; } = RecurrencePattern.None;

    /// <summary>
    /// Giorni della settimana per ricorrenza settimanale (comma-separated: "Monday,Wednesday,Friday")
    /// </summary>
    public string? RecurrenceDays { get; set; }

    /// <summary>
    /// Colore esadecimale per visualizzazione calendario (es: "#4CAF50")
    /// </summary>
    public string Color { get; set; } = "#2196F3";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
