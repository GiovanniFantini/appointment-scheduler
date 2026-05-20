namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Filiale / punto vendita / sede di un merchant.
/// Un merchant ha sempre almeno una filiale: quella creata di default
/// (IsHeadquarters = true). I merchant che non configurano filiali aggiuntive
/// ne hanno comunque esattamente una a DB.
/// </summary>
public class MerchantBranch
{
    public int Id { get; set; }
    public int MerchantId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Codice interno opzionale (es. "MI01").</summary>
    public string? Code { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }

    /// <summary>
    /// True per la sede principale, creata automaticamente alla registrazione
    /// del merchant (o via migration per i merchant esistenti).
    /// </summary>
    public bool IsHeadquarters { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
