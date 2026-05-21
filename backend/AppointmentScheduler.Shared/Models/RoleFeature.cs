using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.Models;

public class RoleFeature
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public MerchantFeature Feature { get; set; }
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Livello di accesso operativo del ruolo alla feature.
    /// Valorizzato solo per la feature Magazzino; null per tutte le altre.
    /// Una feature Magazzino abilitata senza livello esplicito è trattata come ReadOnly.
    /// </summary>
    public FeatureAccessLevel? AccessLevel { get; set; }

    // Navigation properties
    public MerchantRole Role { get; set; } = null!;
}
