namespace AppointmentScheduler.Shared.Models;

/// <summary>
/// Filiale aggiuntiva su cui un dipendente è abilitato a lavorare, oltre alla
/// sua HomeBranch. Modello "ibrido + interscambiabile": il dipendente ha una
/// sede primaria (EmployeeMembership.HomeBranchId) e zero o più sedi extra
/// consentite tramite questa tabella.
///
/// L'assegnazione a un turno fuori da HomeBranch ∪ EmployeeBranchAccess non è
/// vietata: genera un warning non bloccante (ShiftConflictKind.BranchMismatch).
/// </summary>
public class EmployeeBranchAccess
{
    public int Id { get; set; }
    public int MembershipId { get; set; }
    public int BranchId { get; set; }

    // Navigation properties
    public EmployeeMembership Membership { get; set; } = null!;
    public MerchantBranch Branch { get; set; } = null!;
}
