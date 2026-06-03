using System.ComponentModel.DataAnnotations;
using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Obbligatoria per i dipendenti interni. Per le risorse esterne  facoltativa:
    /// se assente il service genera un'email tecnica.
    /// </summary>
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Ruolo applicativo. Se 0/non valido, il service risolve il ruolo base in
    /// base al tipo risorsa (Interno Base / Esterno Base).
    /// </summary>
    public int RoleId { get; set; }
    public List<int> SkillIds { get; set; } = new();

    /// <summary>
    /// Filiale primaria. Se null/0, il service usa la HQ del merchant (mono-sede).
    /// </summary>
    public int? HomeBranchId { get; set; }
    /// <summary>Reparto primario. Null = nessun reparto fisso (Jolly).</summary>
    public int? HomeDepartmentId { get; set; }
    /// <summary>Filiali aggiuntive consentite oltre alla HomeBranch.</summary>
    public List<int> AllowedBranchIds { get; set; } = new();

    /// <summary>
    /// Tipo di risorsa. Default Internal. Per la creazione rapida di un esterno
    /// inline (dal selettore turni) basta valorizzare questo + FirstName/LastName.
    /// </summary>
    public EmployeeKind Kind { get; set; } = EmployeeKind.Internal;

    // ── Anagrafica risorsa esterna — opzionale, usata solo se Kind == External ──
    public ExternalContractType? ContractType { get; set; }
    public string? AgencyName { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? ExternalNotes { get; set; }
}

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Obbligatoria per gli interni; per gli esterni  facoltativa. Alla conversione
    /// esternointerno il service richiede un'email reale (non tecnica).
    /// </summary>
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public List<int> SkillIds { get; set; } = new();

    /// <summary>
    /// Filiale primaria. Se null/0, il service usa la HQ del merchant (mono-sede).
    /// </summary>
    public int? HomeBranchId { get; set; }
    /// <summary>Reparto primario. Null = nessun reparto fisso (Jolly).</summary>
    public int? HomeDepartmentId { get; set; }
    /// <summary>Filiali aggiuntive consentite oltre alla HomeBranch.</summary>
    public List<int> AllowedBranchIds { get; set; } = new();

    /// <summary>Tipo di risorsa. Permette la conversione interno/esterno.</summary>
    public EmployeeKind Kind { get; set; } = EmployeeKind.Internal;

    // ── Anagrafica risorsa esterna — opzionale, usata solo se Kind == External ──
    public ExternalContractType? ContractType { get; set; }
    public string? AgencyName { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? ExternalNotes { get; set; }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool HasUserAccount { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Tipo di risorsa: dipendente interno o risorsa esterna.</summary>
    public EmployeeKind Kind { get; set; } = EmployeeKind.Internal;

    /// <summary>
    /// True se l'email  un indirizzo tecnico generato (@noemail.local) e non un
    /// contatto reale: la UI non deve mostrarlo come email valida.
    /// </summary>
    public bool HasTechnicalEmail { get; set; }

    // ── Anagrafica risorsa esterna — popolati solo se Kind == External ──
    public ExternalContractType? ContractType { get; set; }
    public string? AgencyName { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? ExternalNotes { get; set; }

    // Membership context (quando ritornato in contesto di un merchant)
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public List<string> ActiveFeatures { get; set; } = new();
    public List<EmployeeSkillDto> Skills { get; set; } = new();

    // Branch / Department context
    public int? HomeBranchId { get; set; }
    public string? HomeBranchName { get; set; }
    public int? HomeDepartmentId { get; set; }
    public string? HomeDepartmentName { get; set; }
    public string? HomeDepartmentColor { get; set; }
    /// <summary>Filiali aggiuntive consentite oltre alla HomeBranch.</summary>
    public List<int> AllowedBranchIds { get; set; } = new();
}

public class EmployeeRegisterRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    // Stessa regola di RegisterMerchantRequest: la complessità  in
    // AuthService.ValidatePassword, qui solo lunghezza minima/massima.
    [Required]
    [StringLength(256, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;

    [StringLength(32)]
    public string? PhoneNumber { get; set; }
}
