using System.ComponentModel.DataAnnotations;

namespace AppointmentScheduler.Shared.DTOs;

public class SubscriptionPlanRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? Description { get; set; }
    public int[] Features { get; set; } = [];
    [Range(0, int.MaxValue)]
    public int? MaxEmployees { get; set; }
    [Range(1, int.MaxValue)]
    public int? MaxBranches { get; set; }
    [Range(typeof(long), "0", "9007199254740991")]
    public long? MaxStorageBytes { get; set; }
}

public class AssignSubscriptionRequest
{
    [Range(1, int.MaxValue)]
    public int PlanId { get; set; }
    [Range(1, 3650)]
    public int? TrialDays { get; set; }
}

public class ExtendTrialRequest
{
    [Range(1, 3650)]
    public int Days { get; set; } = 14;
}

public record SubscriptionAccessDto(
    int MerchantId, int? PlanId, string? PlanName, string Status,
    DateTime? TrialEndsAt, string[] ActiveFeatures, Dictionary<string, string> FeatureLevels,
    int? MaxEmployees, int? MaxBranches, long? MaxStorageBytes);
