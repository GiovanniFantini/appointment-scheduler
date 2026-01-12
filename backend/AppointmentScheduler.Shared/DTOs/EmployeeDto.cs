namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per la creazione di un nuovo dipendente
/// </summary>
public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? BadgeCode { get; set; }
    public string? Role { get; set; }
    public string? ShiftsConfiguration { get; set; }
}

/// <summary>
/// DTO per l'aggiornamento di un dipendente esistente
/// </summary>
public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? BadgeCode { get; set; }
    public string? Role { get; set; }
    public string? ShiftsConfiguration { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO per la risposta con i dati del dipendente
/// </summary>
public class EmployeeDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? BadgeCode { get; set; }
    public string? Role { get; set; }
    public string? ShiftsConfiguration { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
