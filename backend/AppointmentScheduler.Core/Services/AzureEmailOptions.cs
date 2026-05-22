namespace AppointmentScheduler.Core.Services;

public sealed class AzureEmailOptions
{
    public string? ConnectionString { get; set; }
    public string SenderAddress { get; set; } = "DoNotReply@azurecomm.net";
    public string SenderDisplayName { get; set; } = "Appointment Scheduler";
}