namespace AppointmentScheduler.Shared.Models;

public class SubscriptionPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int[] Features { get; set; } = [];
    public int? MaxEmployees { get; set; }
    public int? MaxBranches { get; set; }
    public long? MaxStorageBytes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
