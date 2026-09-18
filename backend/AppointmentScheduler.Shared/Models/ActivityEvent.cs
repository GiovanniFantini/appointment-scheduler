namespace AppointmentScheduler.Shared.Models;

public sealed class ActivityEvent
{
    public long Id { get; set; }
    public Guid EventId { get; set; } = Guid.NewGuid();
    public int SchemaVersion { get; set; } = 1;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    public int? MerchantId { get; set; }
    public string Source { get; set; } = "server";
    public string App { get; set; } = "system";
    public string Category { get; set; } = "operation";
    public string Action { get; set; } = "";
    public string Outcome { get; set; } = "committed";
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? RequestId { get; set; }
    public Guid OperationId { get; set; }
    public Guid? SessionId { get; set; }
    public int? StatusCode { get; set; }
    public long? DurationMs { get; set; }
    public string Details { get; set; } = "{}";
}
