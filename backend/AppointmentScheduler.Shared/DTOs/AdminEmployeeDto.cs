using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// Item di lista per la vista sys-admin "Employees".
/// Un employee può esistere senza User collegato (pre-caricato dal merchant, "weak association via email").
/// </summary>
public class AdminEmployeeListItemDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public EmployeeKind Kind { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Numero di merchant a cui l'employee è associato tramite EmployeeMembership.</summary>
    public int MerchantCount { get; set; }

    /// <summary>Nome del primo merchant (se MerchantCount==1) altrimenti null — usato come label rapida.</summary>
    public string? PrimaryMerchantName { get; set; }
    public int? PrimaryMerchantId { get; set; }

    /// <summary>True se l'employee ha già un User registrato (UserId != null).</summary>
    public bool HasAccount { get; set; }
    public int? UserId { get; set; }
}

public class AdminEmployeeDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public EmployeeKind Kind { get; set; }
    public ExternalContractType? ContractType { get; set; }
    public string? AgencyName { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? ExternalNotes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int? UserId { get; set; }
    public bool HasAccount { get; set; }

    public List<AdminEmployeeMembershipDto> Memberships { get; set; } = new();
}

public class AdminEmployeeMembershipDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public bool MerchantApproved { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int HomeBranchId { get; set; }
    public string HomeBranchName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class AdminEmployeeListResponse
{
    public List<AdminEmployeeListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AdminEmployeeListQuery
{
    public int? MerchantId { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>True = solo employee senza User, False = solo con User, null = entrambi.</summary>
    public bool? HasAccount { get; set; }

    public EmployeeKind? Kind { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
