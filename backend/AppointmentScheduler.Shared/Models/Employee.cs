namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Rappresenta un dipendente di un merchant
/// </summary>
public class Employee
{
    public int Id { get; set; }
    public int MerchantId { get; set; } // FK a Merchant

    /// <summary>
    /// FK a User - nullable per backward compatibility con employee già esistenti
    /// Quando l'employee si registra nell'app, viene creato un User collegato
    /// </summary>
    public int? UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Codice badge/identificativo del dipendente (es: "EMP001")
    /// </summary>
    public string? BadgeCode { get; set; }

    /// <summary>
    /// Ruolo del dipendente (es: "Receptionist", "Stylist", "Trainer", etc.)
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Configurazione turni e disponibilità (JSON flessibile)
    /// Es: {"shifts": [{"day": "Monday", "start": "09:00", "end": "17:00"}]}
    /// </summary>
    public string? ShiftsConfiguration { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public User? User { get; set; } // User associato quando l'employee si registra
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    /// <summary>
    /// DEPRECATED: Collection di turni per backward compatibility
    /// </summary>
    [Obsolete("Use ShiftEmployees collection for multi-employee assignment")]
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    /// <summary>
    /// Turni assegnati a questo dipendente (relazione many-to-many)
    /// </summary>
    public ICollection<ShiftEmployee> ShiftEmployees { get; set; } = new List<ShiftEmployee>();

    public ICollection<EmployeeWorkingHoursLimit> WorkingHoursLimits { get; set; } = new List<EmployeeWorkingHoursLimit>();
    public ICollection<OvertimeRecord> OvertimeRecords { get; set; } = new List<OvertimeRecord>();
    public ICollection<ShiftCorrection> ShiftCorrections { get; set; } = new List<ShiftCorrection>();
}
