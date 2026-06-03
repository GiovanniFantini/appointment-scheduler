using System.ComponentModel.DataAnnotations;

namespace AppointmentScheduler.Shared.DTOs;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
