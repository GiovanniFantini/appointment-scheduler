namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Limiti orari di lavoro per dipendente
/// Permette di impostare massimi giornalieri, settimanali e mensili
/// </summary>
public class EmployeeWorkingHoursLimit
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int MerchantId { get; set; }

    /// <summary>
    /// Ore massime giornaliere (es: 8)
    /// </summary>
    public decimal? MaxHoursPerDay { get; set; }

    /// <summary>
    /// Ore massime settimanali (es: 40)
    /// </summary>
    public decimal? MaxHoursPerWeek { get; set; }

    /// <summary>
    /// Ore massime mensili (es: 160)
    /// </summary>
    public decimal? MaxHoursPerMonth { get; set; }

    /// <summary>
    /// Ore minime garantite settimanali (per contratti part-time)
    /// </summary>
    public decimal? MinHoursPerWeek { get; set; }

    /// <summary>
    /// Ore minime garantite mensili
    /// </summary>
    public decimal? MinHoursPerMonth { get; set; }

    /// <summary>
    /// Consenti ore straordinarie oltre i limiti
    /// </summary>
    public bool AllowOvertime { get; set; } = false;

    /// <summary>
    /// Ore straordinarie massime settimanali (se AllowOvertime = true)
    /// </summary>
    public decimal? MaxOvertimeHoursPerWeek { get; set; }

    /// <summary>
    /// Ore straordinarie massime mensili
    /// </summary>
    public decimal? MaxOvertimeHoursPerMonth { get; set; }

    /// <summary>
    /// Data inizio validità limite
    /// </summary>
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data fine validità (nullable = illimitato)
    /// </summary>
    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Merchant Merchant { get; set; } = null!;
}
