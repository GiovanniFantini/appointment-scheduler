namespace AppointmentScheduler.Shared.Models;

public class MerchantRole
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Se true, questo è il ruolo "Responsabile App" con tutte le feature attive.
    /// Creato automaticamente alla registrazione del merchant.
    /// </summary>
    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public ICollection<RoleFeature> Features { get; set; } = new List<RoleFeature>();
    public ICollection<EmployeeMembership> Memberships { get; set; } = new List<EmployeeMembership>();
}
