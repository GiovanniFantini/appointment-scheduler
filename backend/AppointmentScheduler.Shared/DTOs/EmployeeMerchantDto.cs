namespace AppointmentScheduler.Shared.DTOs;

/// <summary>
/// DTO che rappresenta un merchant per cui un employee lavora
/// </summary>
public class EmployeeMerchantDto
{
    public int EmployeeId { get; set; } // ID del record Employee specifico
    public int MerchantId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Role { get; set; } // Ruolo in questo merchant (es: "Stylist", "Receptionist")
    public string? BadgeCode { get; set; }
}
