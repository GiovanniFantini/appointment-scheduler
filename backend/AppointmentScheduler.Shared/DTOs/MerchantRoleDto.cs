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
}

public class AssignRoleRequest
{
    public int EmployeeId { get; set; }
    public int RoleId { get; set; }
}
