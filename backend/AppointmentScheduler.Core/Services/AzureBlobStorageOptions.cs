namespace AppointmentScheduler.Core.Services;

public sealed class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "erp-documents";
    public int SasTokenExpirationMinutes { get; set; } = 5;
}