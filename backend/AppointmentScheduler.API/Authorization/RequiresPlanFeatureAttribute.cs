using AppointmentScheduler.Shared.Enums;

namespace AppointmentScheduler.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresPlanFeatureAttribute(MerchantFeature feature) : Attribute
{
    public MerchantFeature Feature { get; } = feature;
}
