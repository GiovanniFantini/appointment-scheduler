using AppointmentScheduler.Shared.Models;

namespace AppointmentScheduler.Data;

public sealed class ActivityContext
{
    public int? UserId { get; set; }
    public int? MerchantId { get; set; }
    public string App { get; set; } = "system";
    public string Action { get; set; } = "system.save";
    public string? RequestId { get; set; }
    public Guid OperationId { get; set; } = Guid.NewGuid();
    public Guid? SessionId { get; set; }

    public ActivityEvent Create(string category, string action, string outcome) => new()
    {
        UserId = UserId, MerchantId = MerchantId, App = App, Action = action,
        Category = category, Outcome = outcome, RequestId = RequestId,
        OperationId = OperationId, SessionId = SessionId
    };
}
