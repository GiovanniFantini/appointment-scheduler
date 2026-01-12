namespace AppointmentScheduler.Shared.DTOs;

public class VersionResponse
{
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime BuildDate { get; set; }
    public string ApiName { get; set; } = string.Empty;
}
