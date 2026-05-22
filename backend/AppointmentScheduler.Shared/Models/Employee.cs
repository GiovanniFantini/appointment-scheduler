using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class Employee
{
    public int Id { get; set; }

    /// <summary>
    /// FK a User — nullable: l'employee può essere pre-caricato dal merchant prima che si registri.
    /// L'associazione avviene tramite email (weak association).
    /// </summary>
    public int? UserId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Distingue un dipendente interno da una risorsa esterna (a chiamata,
    /// somministrato, libero professionista). Default <see cref="EmployeeKind.Internal"/>:
    /// tutti gli Employee preesistenti restano interni.
    /// </summary>
    public EmployeeKind Kind { get; set; } = EmployeeKind.Internal;

    // ── Anagrafica risorsa esterna ────────────────────────────────────────
    // Tutti opzionali, significativi solo quando Kind == External.

    /// <summary>Tipo di rapporto della risorsa esterna.</summary>
    public ExternalContractType? ContractType { get; set; }

    /// <summary>Agenzia interinale / cooperativa che fornisce la risorsa.</summary>
    public string? AgencyName { get; set; }

    /// <summary>Tariffa oraria concordata, per il futuro foglio ore.</summary>
    public decimal? HourlyRate { get; set; }

    /// <summary>Note libere sulla risorsa esterna.</summary>
    public string? ExternalNotes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<EmployeeMembership> Memberships { get; set; } = new List<EmployeeMembership>();
    public ICollection<EventParticipant> EventParticipations { get; set; } = new List<EventParticipant>();
    public ICollection<HRDocument> HRDocuments { get; set; } = new List<HRDocument>();
    public ICollection<EmployeeRequest> Requests { get; set; } = new List<EmployeeRequest>();
    public ICollection<EmployeeSkill> Skills { get; set; } = new List<EmployeeSkill>();
}
