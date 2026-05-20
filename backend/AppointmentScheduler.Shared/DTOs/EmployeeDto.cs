using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
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
}

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
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
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
