using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.Shared.DTOs;

public class MerchantRoleDto
{
    public int Id { get; set; }
    public int MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public List<RoleFeatureDto> Features { get; set; } = new();
    public int MemberCount { get; set; }
}

public class RoleFeatureDto
{
    public MerchantFeature Feature { get; set; }
    public string FeatureName => Feature.ToString();
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Livello di accesso operativo. Valorizzato solo per la feature Magazzino.
    /// </summary>
    public FeatureAccessLevel? AccessLevel { get; set; }
}

public class CreateMerchantRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public List<MerchantFeatureRequest> Features { get; set; } = new();
}

public class UpdateMerchantRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public List<MerchantFeatureRequest> Features { get; set; } = new();
}

public class MerchantFeatureRequest
{
    public MerchantFeature Feature { get; set; }
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Livello di accesso operativo. Significativo solo per la feature Magazzino.
    /// Se la feature Magazzino è abilitata senza livello esplicito, il default è ReadOnly.
    /// </summary>
    public FeatureAccessLevel? AccessLevel { get; set; }
}

public class AssignRoleRequest
{
    public int EmployeeId { get; set; }
    public int RoleId { get; set; }
}
