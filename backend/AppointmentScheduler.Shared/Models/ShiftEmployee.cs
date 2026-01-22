namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Tabella di associazione many-to-many tra Shift ed Employee
/// Permette l'assegnazione di più dipendenti allo stesso turno
/// </summary>
public class ShiftEmployee
{
    public int Id { get; set; }

    /// <summary>
    /// FK al turno
    /// </summary>
    public int ShiftId { get; set; }

    /// <summary>
    /// FK al dipendente assegnato
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Indica se il dipendente ha confermato questo turno
    /// </summary>
    public bool IsConfirmed { get; set; } = false;

    /// <summary>
    /// Indica se il dipendente ha fatto check-in
    /// </summary>
    public bool IsCheckedIn { get; set; } = false;

    /// <summary>
    /// Timestamp check-in
    /// </summary>
    public DateTime? CheckInTime { get; set; }

    /// <summary>
    /// Indica se il dipendente ha fatto check-out
    /// </summary>
    public bool IsCheckedOut { get; set; } = false;

    /// <summary>
    /// Timestamp check-out
    /// </summary>
    public DateTime? CheckOutTime { get; set; }

    /// <summary>
    /// Posizione GPS al check-in (opzionale, formato JSON)
    /// </summary>
    public string? CheckInLocation { get; set; }

    /// <summary>
    /// Posizione GPS al check-out (opzionale, formato JSON)
    /// </summary>
    public string? CheckOutLocation { get; set; }

    /// <summary>
    /// Note specifiche per questo dipendente su questo turno
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Shift Shift { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
