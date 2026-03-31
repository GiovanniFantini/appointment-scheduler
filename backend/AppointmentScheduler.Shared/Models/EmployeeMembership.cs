namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Associazione employee ↔ merchant con ruolo assegnato.
/// Un employee può essere membro di più merchant.
/// </summary>
public class EmployeeMembership
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int MerchantId { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Merchant Merchant { get; set; } = null!;
    public MerchantRole Role { get; set; } = null!;
}
