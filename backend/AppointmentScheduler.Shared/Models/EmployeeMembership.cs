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

    /// <summary>
    /// Filiale primaria del dipendente. Sempre valorizzata: i merchant mono-sede
    /// puntano alla loro unica filiale HQ.
    /// </summary>
    public int HomeBranchId { get; set; }

    /// <summary>
    /// Reparto primario del dipendente. Null = nessun reparto fisso (Jolly).
    /// </summary>
    public int? HomeDepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Employee Employee { get; set; } = null!;
    public Merchant Merchant { get; set; } = null!;
    public MerchantRole Role { get; set; } = null!;
    public MerchantBranch HomeBranch { get; set; } = null!;
    public Department? HomeDepartment { get; set; }

    /// <summary>Filiali aggiuntive su cui il dipendente è abilitato, oltre a HomeBranch.</summary>
    public ICollection<EmployeeBranchAccess> BranchAccess { get; set; } = new List<EmployeeBranchAccess>();
}
