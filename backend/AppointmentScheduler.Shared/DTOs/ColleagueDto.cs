namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO per visualizzare le informazioni dei colleghi (employee dello stesso merchant)
/// </summary>
public class ColleagueDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? BadgeCode { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
}
