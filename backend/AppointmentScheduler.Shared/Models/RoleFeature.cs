using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class RoleFeature
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public MerchantFeature Feature { get; set; }
    public bool IsEnabled { get; set; } = false;

    // Navigation properties
    public MerchantRole Role { get; set; } = null!;
}
