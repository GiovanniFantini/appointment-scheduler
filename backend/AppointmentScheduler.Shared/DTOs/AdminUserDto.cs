using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

/// <summary>Item di lista nella vista sys-admin "Utenti piattaforma".</summary>
public class AdminUserListItemDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public bool IsActive { get; set; }
    public int? MerchantId { get; set; }
    public string? MerchantName { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Dettaglio utente sys-admin: include account, link a merchant/employee.</summary>
public class AdminUserDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public AccountType AccountType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public AdminMerchantLinkDto? Merchant { get; set; }
    public AdminEmployeeLinkDto? Employee { get; set; }
}

public class AdminMerchantLinkDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
}

public class AdminEmployeeLinkDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public EmployeeKind Kind { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Risposta paginata della lista utenti.</summary>
public class AdminUserListResponse
{
    public List<AdminUserListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>Filtri della lista utenti sys-admin.</summary>
public class AdminUserListQuery
{
    public AccountType? AccountType { get; set; }
    public bool? IsActive { get; set; }
    public int? MerchantId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
