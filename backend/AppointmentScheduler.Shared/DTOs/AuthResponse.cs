using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }

    // Merchant account
    public int? MerchantId { get; set; }

    // Employee: lista aziende disponibili (pre-switch)
    public int? EmployeeId { get; set; }
    public List<EmployeeCompanyDto> Companies { get; set; } = new();

    // Features attive (post company-switch)
    public List<string> ActiveFeatures { get; set; } = new();
}

/// <summary>
/// Azienda disponibile per l'employee (usata nel select-company screen).
/// </summary>
public class EmployeeCompanyDto
{
    public int MerchantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? City { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}
